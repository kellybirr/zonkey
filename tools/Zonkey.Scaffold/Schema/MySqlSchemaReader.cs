using System.Text.RegularExpressions;
using MySqlConnector;

namespace Zonkey.Scaffold.Schema;

/// <summary>
/// Reads schema metadata from MySQL/MariaDB via information_schema. In MySQL a "schema" is a
/// database -- there is no separate namespacing layer above it the way PostgreSQL and SQL Server
/// have -- so every query here filters table_schema to the requested database name(s) directly.
/// </summary>
public sealed partial class MySqlSchemaReader(string connectionString) : ISchemaReader
{
    public string Provider => "mysql";

    // Captures the display width MySQL still reports for TINYINT(1) in column_type, e.g.
    // "tinyint(1)". Since MySQL 8.0.19, integer display width is dropped from column_type for
    // every other width (TINYINT(4) reports column_type "tinyint", not "tinyint(4)") -- it
    // survives only for width 1, precisely because that is the shape BOOLEAN aliases to, and MySQL
    // keeps it for readability. Confirmed live: this survival is *also* conditional on signedness
    // -- "TINYINT(1) UNSIGNED" reports column_type "tinyint unsigned" with no parenthesized width
    // at all, identically to plain "TINYINT UNSIGNED". So the width-1 signal this regex looks for
    // only ever exists for the signed form; a tinyint with no parenthesized width, or an unsigned
    // one regardless of its declared width, must never be mistaken for the width-1 case. See
    // MySqlTypeMapper's handling of ColumnInfo.IsUnsigned for how the unsigned, width-unrecoverable
    // case is resolved.
    [GeneratedRegex(@"^tinyint\((?<width>\d+)\)")]
    private static partial Regex TinyIntWidthRegex();

    public async Task<IReadOnlyList<string>> GetNonSystemSchemas(CancellationToken ct)
    {
        await using var cnxn = new MySqlConnection(connectionString);
        await cnxn.OpenAsync(ct);

        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT schema_name FROM information_schema.schemata
            WHERE schema_name NOT IN ('mysql','information_schema','performance_schema','sys')
            ORDER BY schema_name
            """;

        var result = new List<string>();
        await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(reader.GetString(0));

        return result;
    }

    public async Task<DatabaseSchema> Read(IReadOnlyList<string> schemas, CancellationToken ct)
    {
        await using var cnxn = new MySqlConnection(connectionString);
        await cnxn.OpenAsync(ct);

        var schema = new DatabaseSchema { Provider = Provider, ServerVersion = cnxn.ServerVersion };
        var index = new Dictionary<string, TableInfo>(StringComparer.Ordinal);

        await ReadTables(cnxn, schemas, schema, index, ct);
        await ReadColumns(cnxn, schemas, index, ct);
        await ReadPrimaryKeys(cnxn, schemas, index, ct);
        await ReadUniqueConstraints(cnxn, schemas, index, ct);
        await ReadForeignKeys(cnxn, schemas, index, ct);

        // Deterministic output: identical schema must produce identical files.
        schema.Tables = schema.Tables
            .OrderBy(t => t.Schema, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        return schema;
    }

    /// <summary>
    /// Builds a parameterized "IN (@s0, @s1, ...)" clause. MySqlConnector, unlike Npgsql, has no
    /// array parameter support (no ANY(@schemas)), so a fixed schema list has to be expanded into
    /// individually named parameters instead.
    /// </summary>
    private static string AddSchemaList(MySqlCommand cmd, IReadOnlyList<string> schemas)
    {
        var names = new string[schemas.Count];
        for (int i = 0; i < schemas.Count; i++)
        {
            names[i] = $"@s{i}";
            cmd.Parameters.AddWithValue(names[i], schemas[i]);
        }

        return string.Join(", ", names);
    }

    private static async Task ReadTables(
        MySqlConnection cnxn, IReadOnlyList<string> schemas,
        DatabaseSchema schema, Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        string inClause = AddSchemaList(cmd, schemas);
        cmd.CommandText = $"""
            SELECT table_schema, table_name, table_type
            FROM information_schema.tables
            WHERE table_schema IN ({inClause})
              AND table_type IN ('BASE TABLE','VIEW')
            ORDER BY table_schema, table_name
            """;

        await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var table = new TableInfo
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1),
                Kind = reader.GetString(2) == "VIEW" ? TableKind.View : TableKind.Table
            };

            schema.Tables.Add(table);
            index[table.QualifiedName] = table;
        }
    }

    private static async Task ReadColumns(
        MySqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        string inClause = AddSchemaList(cmd, schemas);
        cmd.CommandText = $"""
            SELECT table_schema, table_name, column_name, ordinal_position,
                   data_type, is_nullable, character_maximum_length,
                   numeric_precision, numeric_scale, extra, column_type
            FROM information_schema.columns
            WHERE table_schema IN ({inClause})
            ORDER BY table_schema, table_name, ordinal_position
            """;

        await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string dataType = reader.GetString(4);
            string extra = reader.IsDBNull(9) ? "" : reader.GetString(9);
            int? precision = reader.IsDBNull(7) ? null : reader.GetInt32(7);

            // data_type never carries sign -- MySQL reports "int" for both INT and INT UNSIGNED --
            // so the "unsigned" marker has to be read out of column_type instead, for every integer
            // type, not just tinyint. This matters beyond cosmetics: an unsigned column's real range
            // (e.g. INT UNSIGNED up to ~4.29e9) exceeds its signed CLR counterpart's range (Int32 up
            // to ~2.15e9), so a mapper that ignores this silently truncates/overflows real data.
            string columnType = reader.IsDBNull(10) ? "" : reader.GetString(10);
            bool isUnsigned = columnType.Contains("unsigned", StringComparison.Ordinal);

            // tinyint's numeric_precision is always 3 (the decimal digit count of a byte), which
            // carries no information about whether this is BOOLEAN's TINYINT(1) alias. The display
            // width that actually distinguishes them lives in column_type instead -- and, per
            // TinyIntWidthRegex's comment, only ever appears there for the signed form.
            if (dataType == "tinyint")
            {
                Match widthMatch = TinyIntWidthRegex().Match(columnType);
                precision = widthMatch.Success ? int.Parse(widthMatch.Groups["width"].Value) : null;
            }

            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(2),
                Ordinal = reader.GetInt32(3) - 1,
                NativeType = dataType,
                IsNullable = reader.GetString(5) == "YES",
                IsUnsigned = isUnsigned,
                MaxLength = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Precision = precision,
                Scale = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                // MySQL has no separate "is_identity" flag or default-expression convention the
                // way PostgreSQL's serial/nextval does -- EXTRA reporting 'auto_increment' is the
                // one and only signal.
                IsIdentity = extra.Contains("auto_increment", StringComparison.Ordinal)
            });
        }
    }

    private static async Task ReadPrimaryKeys(
        MySqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        string inClause = AddSchemaList(cmd, schemas);
        cmd.CommandText = $"""
            SELECT table_schema, table_name, column_name
            FROM information_schema.key_column_usage
            WHERE constraint_name = 'PRIMARY' AND table_schema IN ({inClause})
            ORDER BY table_schema, table_name, ordinal_position
            """;

        await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            table.PrimaryKey.Add(reader.GetString(2));
        }
    }

    private static async Task ReadUniqueConstraints(
        MySqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        string inClause = AddSchemaList(cmd, schemas);
        cmd.CommandText = $"""
            SELECT table_schema, table_name, index_name, column_name
            FROM information_schema.statistics
            WHERE non_unique = 0 AND index_name <> 'PRIMARY' AND table_schema IN ({inClause})
            ORDER BY table_schema, table_name, index_name, seq_in_index
            """;

        await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string indexName = reader.GetString(2);
            UniqueConstraintInfo? existing =
                table.UniqueConstraints.FirstOrDefault(u => u.Name == indexName);

            if (existing is null)
            {
                existing = new UniqueConstraintInfo { Name = indexName };
                table.UniqueConstraints.Add(existing);
            }

            existing.Columns.Add(reader.GetString(3));
        }
    }

    private static async Task ReadForeignKeys(
        MySqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        string inClause = AddSchemaList(cmd, schemas);
        cmd.CommandText = $"""
            SELECT table_schema, table_name, constraint_name,
                   referenced_table_schema, referenced_table_name,
                   column_name, referenced_column_name
            FROM information_schema.key_column_usage
            WHERE referenced_table_name IS NOT NULL AND table_schema IN ({inClause})
            ORDER BY table_schema, table_name, constraint_name, ordinal_position
            """;

        await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
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
