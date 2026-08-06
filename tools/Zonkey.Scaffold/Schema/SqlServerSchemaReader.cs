using Microsoft.Data.SqlClient;

namespace Zonkey.Scaffold.Schema;

/// <summary>
/// Reads schema metadata from SQL Server via sys.tables/sys.columns/sys.indexes/sys.foreign_keys --
/// the same catalog-view shape every other provider in this tool uses. This retires SMO
/// (tools/Zonkey.CodeGen's dependency) from the repository entirely: SMO required a client-side
/// install and modeled far more than a scaffolder needs, where these views are just SQL.
/// </summary>
public sealed class SqlServerSchemaReader(string connectionString) : ISchemaReader
{
    public string Provider => "sqlserver";

    public async Task<IReadOnlyList<string>> GetNonSystemSchemas(CancellationToken ct)
    {
        await using var cnxn = new SqlConnection(connectionString);
        await cnxn.OpenAsync(ct);

        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT name FROM sys.schemas
            WHERE name NOT IN (
                'sys', 'INFORMATION_SCHEMA', 'guest',
                'db_owner', 'db_accessadmin', 'db_securityadmin', 'db_ddladmin',
                'db_backupoperator', 'db_datareader', 'db_datawriter',
                'db_denydatareader', 'db_denydatawriter')
            ORDER BY name
            """;

        var result = new List<string>();
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(reader.GetString(0));

        return result;
    }

    public async Task<DatabaseSchema> Read(IReadOnlyList<string> schemas, CancellationToken ct)
    {
        await using var cnxn = new SqlConnection(connectionString);
        await cnxn.OpenAsync(ct);

        var schema = new DatabaseSchema { Provider = Provider, ServerVersion = cnxn.ServerVersion };
        var index = new Dictionary<string, TableInfo>(StringComparer.Ordinal);

        await ReadTables(cnxn, schemas, schema, index, ct);
        await ReadColumns(cnxn, schemas, index, ct);
        await ReadKeysAndConstraints(cnxn, schemas, index, ct);
        await ReadForeignKeys(cnxn, schemas, index, ct);

        // Deterministic output: identical schema must produce identical files.
        schema.Tables = schema.Tables
            .OrderBy(t => t.Schema, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        return schema;
    }

    /// <summary>
    /// Joins the requested schema list into a single comma-separated parameter consumed by
    /// STRING_SPLIT. Microsoft.Data.SqlClient, like MySqlConnector, has no array parameter support
    /// (no ANY(@schemas) the way Npgsql has), so a fixed schema list is passed as one string and
    /// exploded server-side instead of building one placeholder per schema.
    /// </summary>
    private static void AddSchemaListParam(SqlCommand cmd, IReadOnlyList<string> schemas)
        => cmd.Parameters.AddWithValue("@schemas", string.Join(",", schemas));

    private const string SchemaListPredicate =
        "s.name IN (SELECT value FROM STRING_SPLIT(@schemas, ','))";

    private static async Task ReadTables(
        SqlConnection cnxn, IReadOnlyList<string> schemas,
        DatabaseSchema schema, Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        AddSchemaListParam(cmd, schemas);
        cmd.CommandText = $"""
            SELECT s.name AS schema_name, t.name AS table_name,
                   CASE WHEN t.type = 'U' THEN 'U' ELSE 'V' END AS obj_type
            FROM sys.objects t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.type IN ('U','V') AND {SchemaListPredicate}
            ORDER BY s.name, t.name
            """;

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var table = new TableInfo
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1),
                Kind = reader.GetString(2) == "V" ? TableKind.View : TableKind.Table
            };

            schema.Tables.Add(table);
            index[table.QualifiedName] = table;
        }
    }

    // The column query carries three SQL Server-specific corrections:
    //
    // 1. max_length is -1 for the (MAX) types (nvarchar(max), varchar(max), varbinary(max)); that
    //    must become MaxLength = null, not -1, or the emitter writes "Length = -1" into the
    //    generated [DataField] attribute -- worse than omitting Length entirely.
    // 2. max_length is reported in BYTES, not characters. nvarchar/nchar/ntext are UCS-2 (2 bytes
    //    per character), so those three must be halved or every Unicode column reports double its
    //    real character length.
    // 3. rowversion/timestamp sets IsRowVersion, which Zonkey uses for optimistic concurrency. Note
    //    (confirmed against a live SQL Server 2022 container): sys.types never actually reports the
    //    name "rowversion" for a column declared ROWVERSION -- it reports "timestamp", ROWVERSION's
    //    underlying/legacy name, since the two are the same type (a table cannot have one of each).
    //    Both spellings are still matched here defensively, and SqlServerTypeMapper matches both too
    //    since its unit tests construct a ColumnInfo with NativeType "rowversion" directly.
    // 4. text/ntext/image report max_length = 16 -- an in-row LOB pointer stub, not the data's
    //    length -- and must be nulled the same as the (MAX) types; see the comment in ReadColumns.
    private const string ColumnSql = """
        SELECT  s.name  AS schema_name,
                t.name  AS table_name,
                c.name  AS column_name,
                c.column_id,
                ty.name AS type_name,
                c.is_nullable,
                c.max_length,
                c.precision,
                c.scale,
                c.is_identity
        FROM sys.columns c
        JOIN sys.objects t  ON t.object_id = c.object_id
        JOIN sys.schemas s  ON s.schema_id = t.schema_id
        JOIN sys.types ty   ON ty.user_type_id = c.user_type_id
        WHERE t.type IN ('U','V') AND {0}
        ORDER BY s.name, t.name, c.column_id
        """;

    private static async Task ReadColumns(
        SqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        AddSchemaListParam(cmd, schemas);
        cmd.CommandText = string.Format(ColumnSql, SchemaListPredicate);

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string typeName = reader.GetString(4);
            short maxLength = reader.GetInt16(6);

            // text/ntext/image are the pre-(MAX) legacy LOB types. Unlike varchar(max)/
            // nvarchar(max)/varbinary(max), which report max_length = -1 (handled below), these
            // three report max_length = 16 -- confirmed live -- the size of the in-row pointer
            // stub SQL Server stores for an off-row LOB value, not the data's length. Left
            // unhandled, that 16 (or, for ntext, 16 halved into an equally bogus 8 by the Unicode
            // check below) reaches ScaffoldPipeline -> CSharpEntityEmitter as a real Length on the
            // emitted [DataField], which DataClassCommandBuilder then uses to size the ADO.NET
            // parameter -- silently truncating or throwing on any write longer than that stub
            // value. There is no dedicated marker in sys.types or sys.columns for "this
            // max_length is a stub, not a length" (no is_lob/is_max-style column exists for these
            // three system types), so -- matching the style already used for the Unicode-halving
            // check immediately below -- the three names are matched literally. This check must
            // run before that halving check, or ntext's stub value gets halved on the way past.
            int? length = typeName switch
            {
                "text" or "ntext" or "image" => null,
                _ => maxLength switch
                {
                    -1 => null,
                    _ when typeName is "nvarchar" or "nchar" => maxLength / 2,
                    _ => maxLength
                }
            };

            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(2),
                Ordinal = reader.GetInt32(3) - 1,
                NativeType = typeName,
                IsNullable = reader.GetBoolean(5),
                MaxLength = length,
                Precision = reader.GetByte(7),
                Scale = reader.GetByte(8),
                IsIdentity = reader.GetBoolean(9),
                IsRowVersion = typeName is "rowversion" or "timestamp"
            });
        }
    }

    private static async Task ReadKeysAndConstraints(
        SqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        AddSchemaListParam(cmd, schemas);
        cmd.CommandText = $"""
            SELECT s.name AS schema_name, t.name AS table_name, i.name AS index_name,
                   i.is_primary_key, c.name AS column_name, ic.key_ordinal
            FROM sys.indexes i
            JOIN sys.objects t  ON t.object_id = i.object_id
            JOIN sys.schemas s  ON s.schema_id = t.schema_id
            JOIN sys.index_columns ic
                ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c
                ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE (i.is_primary_key = 1 OR i.is_unique_constraint = 1) AND {SchemaListPredicate}
            ORDER BY s.name, t.name, i.name, ic.key_ordinal
            """;

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string indexName = reader.GetString(2);
            bool isPrimaryKey = reader.GetBoolean(3);
            string column = reader.GetString(4);

            if (isPrimaryKey)
            {
                table.PrimaryKey.Add(column);
            }
            else
            {
                UniqueConstraintInfo? existing =
                    table.UniqueConstraints.FirstOrDefault(u => u.Name == indexName);

                if (existing is null)
                {
                    existing = new UniqueConstraintInfo { Name = indexName };
                    table.UniqueConstraints.Add(existing);
                }

                existing.Columns.Add(column);
            }
        }
    }

    private static async Task ReadForeignKeys(
        SqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        AddSchemaListParam(cmd, schemas);
        cmd.CommandText = $"""
            SELECT s.name AS schema_name, t.name AS table_name, fk.name AS fk_name,
                   rs.name AS ref_schema, rt.name AS ref_table,
                   c.name AS column_name, rc.name AS ref_column_name,
                   fkc.constraint_column_id
            FROM sys.foreign_keys fk
            JOIN sys.objects t   ON t.object_id = fk.parent_object_id
            JOIN sys.schemas s   ON s.schema_id = t.schema_id
            JOIN sys.objects rt  ON rt.object_id = fk.referenced_object_id
            JOIN sys.schemas rs  ON rs.schema_id = rt.schema_id
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.columns c
                ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            JOIN sys.columns rc
                ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
            WHERE {SchemaListPredicate}
            ORDER BY s.name, t.name, fk.name, fkc.constraint_column_id
            """;

        await using SqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string name = reader.GetString(2);
            ForeignKeyInfo? fk = table.ForeignKeys.FirstOrDefault(f => f.Name == name);

            if (fk is null)
            {
                fk = new ForeignKeyInfo
                {
                    Name = name,
                    ReferencedSchema = reader.GetString(3),
                    ReferencedTable = reader.GetString(4)
                };
                table.ForeignKeys.Add(fk);
            }

            fk.Columns.Add(reader.GetString(5));
            fk.ReferencedColumns.Add(reader.GetString(6));
        }
    }
}
