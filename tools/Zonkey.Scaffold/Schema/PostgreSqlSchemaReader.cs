using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace Zonkey.Scaffold.Schema;

/// <summary>
/// Reads schema metadata from PostgreSQL via information_schema plus the pg_catalog tables for
/// constraint detail (information_schema's constraint views don't preserve column order).
/// </summary>
public sealed partial class PostgreSqlSchemaReader(string connectionString) : ISchemaReader
{
    public string Provider => "postgresql";

    // Captures the raw regclass string-literal argument to nextval(...) without trying to also
    // parse identifier quoting here — quoting (each of schema and sequence independently quoted or
    // bare) is handled afterwards by ExtractSequenceName, because a character class that simply
    // excludes '"' can't match the doubly-quoted schema-qualified form Postgres emits for a serial
    // column in a mixed-case schema, e.g. nextval('"MixedSchema"."MixedSeq"'::regclass).
    [GeneratedRegex(@"nextval\('(?<expr>.+?)'::regclass\)")]
    private static partial Regex NextValRegex();

    public async Task<IReadOnlyList<string>> GetNonSystemSchemas(CancellationToken ct)
    {
        await using var cnxn = new NpgsqlConnection(connectionString);
        await cnxn.OpenAsync(ct);

        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT nspname FROM pg_catalog.pg_namespace
            WHERE nspname NOT IN ('pg_catalog','information_schema')
              AND nspname NOT LIKE 'pg\_%'
            ORDER BY nspname
            """;

        var result = new List<string>();
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(reader.GetString(0));

        return result;
    }

    public async Task<DatabaseSchema> Read(IReadOnlyList<string> schemas, CancellationToken ct)
    {
        await using var cnxn = new NpgsqlConnection(connectionString);
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

    private static async Task ReadTables(
        NpgsqlConnection cnxn, IReadOnlyList<string> schemas,
        DatabaseSchema schema, Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT table_schema, table_name, table_type
            FROM information_schema.tables
            WHERE table_schema = ANY(@schemas)
              AND table_type IN ('BASE TABLE','VIEW')
            ORDER BY table_schema, table_name
            """;
        cmd.Parameters.AddWithValue("schemas", schemas.ToArray());

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
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
        NpgsqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT table_schema, table_name, column_name, ordinal_position,
                   data_type, is_nullable, character_maximum_length,
                   numeric_precision, numeric_scale, column_default, is_identity
            FROM information_schema.columns
            WHERE table_schema = ANY(@schemas)
            ORDER BY table_schema, table_name, ordinal_position
            """;
        cmd.Parameters.AddWithValue("schemas", schemas.ToArray());

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string? columnDefault = reader.IsDBNull(9) ? null : reader.GetString(9);
            bool isIdentityColumn = !reader.IsDBNull(10) && reader.GetString(10) == "YES";

            // serial/bigserial are sugar for a column default of nextval(...) — not is_identity —
            // so both forms have to be recognised or every serial key silently loses IsAutoIncrement.
            Match nextval = columnDefault is null ? Match.Empty : NextValRegex().Match(columnDefault);

            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(2),
                Ordinal = reader.GetInt32(3) - 1,
                NativeType = reader.GetString(4),
                IsNullable = reader.GetString(5) == "YES",
                MaxLength = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Precision = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                Scale = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                IsIdentity = isIdentityColumn || nextval.Success,
                SequenceName = nextval.Success ? ExtractSequenceName(nextval.Groups["expr"].Value) : null
            });
        }
    }

    /// <summary>
    /// The nextval(...) argument is a regclass string literal holding a (possibly schema-qualified)
    /// identifier, where each part may independently be unquoted or double-quoted — e.g. a serial
    /// column in a mixed-case schema not on the search path produces
    /// nextval('"MixedSchema"."MixedSeq"'::regclass). Splits on dots that aren't inside a quoted
    /// part, keeps only the last (sequence) part, and un-quotes it — including un-escaping a
    /// doubled internal quote ("" -> ") — the same way Postgres itself parses a quoted identifier.
    /// An unquoted part needs no such handling since it can't contain a dot or a quote.
    /// </summary>
    private static string ExtractSequenceName(string expr)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < expr.Length && expr[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == '.' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        parts.Add(current.ToString());
        return parts[^1];
    }

    private static async Task ReadKeysAndConstraints(
        NpgsqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT n.nspname, c.relname, con.conname, con.contype,
                   a.attname, array_position(con.conkey, a.attnum) AS key_position
            FROM pg_constraint con
            JOIN pg_class c ON c.oid = con.conrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN unnest(con.conkey) AS k(attnum) ON true
            JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = k.attnum
            WHERE n.nspname = ANY(@schemas) AND con.contype IN ('p','u')
            ORDER BY n.nspname, c.relname, con.conname, key_position
            """;
        cmd.Parameters.AddWithValue("schemas", schemas.ToArray());

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string qualified = $"{reader.GetString(0)}.{reader.GetString(1)}";
            if (!index.TryGetValue(qualified, out TableInfo? table)) continue;

            string constraintName = reader.GetString(2);
            char type = reader.GetChar(3);
            string column = reader.GetString(4);

            if (type == 'p')
            {
                table.PrimaryKey.Add(column);
            }
            else
            {
                UniqueConstraintInfo? existing =
                    table.UniqueConstraints.FirstOrDefault(u => u.Name == constraintName);

                if (existing is null)
                {
                    existing = new UniqueConstraintInfo { Name = constraintName };
                    table.UniqueConstraints.Add(existing);
                }

                existing.Columns.Add(column);
            }
        }
    }

    private static async Task ReadForeignKeys(
        NpgsqlConnection cnxn, IReadOnlyList<string> schemas,
        Dictionary<string, TableInfo> index, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT n.nspname, c.relname, con.conname,
                   fn.nspname AS ref_schema, fc.relname AS ref_table,
                   a.attname, fa.attname AS ref_column, ord.n AS position
            FROM pg_constraint con
            JOIN pg_class c ON c.oid = con.conrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_class fc ON fc.oid = con.confrelid
            JOIN pg_namespace fn ON fn.oid = fc.relnamespace
            JOIN generate_subscripts(con.conkey, 1) AS ord(n) ON true
            JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = con.conkey[ord.n]
            JOIN pg_attribute fa ON fa.attrelid = fc.oid AND fa.attnum = con.confkey[ord.n]
            WHERE n.nspname = ANY(@schemas) AND con.contype = 'f'
            ORDER BY n.nspname, c.relname, con.conname, ord.n
            """;
        cmd.Parameters.AddWithValue("schemas", schemas.ToArray());

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
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
