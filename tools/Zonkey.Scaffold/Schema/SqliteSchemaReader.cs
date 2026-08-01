using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;

namespace Zonkey.Scaffold.Schema;

/// <summary>
/// SQLite has no schema concept, so it reports a single implicit schema named "main".
/// Metadata comes from sqlite_master plus the PRAGMA family.
/// </summary>
public sealed class SqliteSchemaReader(string connectionString) : ISchemaReader
{
    public const string ImplicitSchema = "main";

    public string Provider => "sqlite";

    public Task<IReadOnlyList<string>> GetNonSystemSchemas(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<string>>([ImplicitSchema]);

    public async Task<DatabaseSchema> Read(IReadOnlyList<string> schemas, CancellationToken ct)
    {
        await using var cnxn = new SqliteConnection(connectionString);
        await cnxn.OpenAsync(ct);

        var schema = new DatabaseSchema
        {
            Provider = Provider,
            ServerVersion = cnxn.ServerVersion
        };

        foreach ((string name, TableKind kind) in await ReadTableList(cnxn, ct))
        {
            var table = new TableInfo { Schema = ImplicitSchema, Name = name, Kind = kind };

            await ReadColumns(cnxn, table, ct);
            await ReadForeignKeys(cnxn, table, ct);
            await ReadUniqueConstraints(cnxn, table, ct);

            if (table.PrimaryKey.Count == 1)
                await MarkAutoIncrement(cnxn, table, ct);

            schema.Tables.Add(table);
        }

        // Foreign keys declared without an explicit column list (e.g. "REFERENCES parent")
        // implicitly target the parent's primary key. Resolving that requires every table's
        // columns to already be read, so it happens as a second pass over the full set.
        ResolveImplicitForeignKeyTargets(schema);

        // Deterministic output: identical schema must produce identical files.
        schema.Tables = schema.Tables.OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
        return schema;
    }

    /// <summary>
    /// Resolves foreign keys whose PRAGMA foreign_key_list "to" column came back NULL for every
    /// member column — SQLite's signal that no column list was given in the FK clause, so the
    /// reference is implicitly to the parent table's primary key, matched positionally.
    /// If the parent table isn't in this read's result set, or its primary key arity doesn't match
    /// the FK's column count, the FK is dropped rather than emitted with mismatched Columns/
    /// ReferencedColumns lengths, which downstream code pairs up positionally.
    /// </summary>
    private static void ResolveImplicitForeignKeyTargets(DatabaseSchema schema)
    {
        var tablesByName = schema.Tables.ToDictionary(t => t.Name, t => t, StringComparer.Ordinal);

        foreach (TableInfo table in schema.Tables)
        {
            var resolved = new List<ForeignKeyInfo>();

            foreach (ForeignKeyInfo fk in table.ForeignKeys)
            {
                if (fk.ReferencedColumns.Count > 0)
                {
                    resolved.Add(fk);
                    continue;
                }

                if (tablesByName.TryGetValue(fk.ReferencedTable, out TableInfo? referenced) &&
                    referenced.PrimaryKey.Count == fk.Columns.Count)
                {
                    fk.ReferencedColumns.AddRange(referenced.PrimaryKey);
                    resolved.Add(fk);
                }
            }

            table.ForeignKeys = resolved;
        }
    }

    private static async Task<List<(string Name, TableKind Kind)>> ReadTableList(
        SqliteConnection cnxn, CancellationToken ct)
    {
        var result = new List<(string, TableKind)>();

        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            SELECT name, type FROM sqlite_master
            WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """;

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string name = reader.GetString(0);
            TableKind kind = reader.GetString(1) == "view" ? TableKind.View : TableKind.Table;
            result.Add((name, kind));
        }

        return result;
    }

    private static async Task ReadColumns(SqliteConnection cnxn, TableInfo table, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({Quote(table.Name)})";

        var keyOrder = new List<(int Position, string Column)>();

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            int cid = reader.GetInt32(0);
            string name = reader.GetString(1);
            string declaredType = reader.IsDBNull(2) ? "" : reader.GetString(2);
            bool notNull = reader.GetInt32(3) != 0;
            int pkPosition = reader.GetInt32(5);   // 0 = not part of PK, else 1-based ordinal

            (string bare, int? length, int? precision, int? scale) = SplitDeclaredType(declaredType);

            table.Columns.Add(new ColumnInfo
            {
                Name = name,
                NativeType = bare,
                IsNullable = !notNull && pkPosition == 0,
                MaxLength = length,
                Precision = precision,
                Scale = scale,
                Ordinal = cid
            });

            if (pkPosition > 0) keyOrder.Add((pkPosition, name));
        }

        table.PrimaryKey = keyOrder.OrderBy(k => k.Position).Select(k => k.Column).ToList();
    }

    /// <summary>Splits "VARCHAR(100)" or "DECIMAL(18,2)" into its parts.</summary>
    private static (string Bare, int? Length, int? Precision, int? Scale) SplitDeclaredType(string declared)
    {
        int open = declared.IndexOf('(');
        if (open < 0) return (declared.Trim().ToUpperInvariant(), null, null, null);

        string bare = declared[..open].Trim().ToUpperInvariant();
        int close = declared.IndexOf(')', open);
        if (close < 0) return (bare, null, null, null);

        string[] parts = declared[(open + 1)..close]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1 && int.TryParse(parts[0], out int len))
            return (bare, len, null, null);

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int p) && int.TryParse(parts[1], out int s))
            return (bare, null, p, s);

        return (bare, null, null, null);
    }

    private static async Task ReadForeignKeys(SqliteConnection cnxn, TableInfo table, CancellationToken ct)
    {
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = $"PRAGMA foreign_key_list({Quote(table.Name)})";

        // Group raw rows by fk id first and order by "seq" before building the final lists —
        // seq is the FK clause's declared column position; the row order returned by the PRAGMA
        // is not itself a documented guarantee.
        var byId = new SortedDictionary<int, (string ReferencedTable, List<(int Seq, string From, string? To)> Rows)>();

        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            int id = reader.GetInt32(0);
            int seq = reader.GetInt32(1);
            string referencedTable = reader.GetString(2);
            string fromColumn = reader.GetString(3);
            // NULL "to" means the FK clause named no column list, e.g. "REFERENCES parent" —
            // SQLite implicitly targets the parent's primary key. Resolved in a later pass once
            // every table's columns (and thus primary keys) have been read.
            string? toColumn = reader.IsDBNull(4) ? null : reader.GetString(4);

            if (!byId.TryGetValue(id, out var entry))
            {
                entry = (referencedTable, new List<(int, string, string?)>());
                byId[id] = entry;
            }

            entry.Rows.Add((seq, fromColumn, toColumn));
        }

        var foreignKeys = new List<ForeignKeyInfo>();
        foreach ((int id, (string referencedTable, List<(int Seq, string From, string? To)> rows)) in byId)
        {
            List<(int Seq, string From, string? To)> ordered = rows.OrderBy(r => r.Seq).ToList();

            var fk = new ForeignKeyInfo
            {
                Name = $"FK_{table.Name}_{referencedTable}_{id}",
                ReferencedSchema = ImplicitSchema,
                ReferencedTable = referencedTable
            };
            fk.Columns.AddRange(ordered.Select(r => r.From));

            // Either every row in the group has a "to" value or none do — SQL requires the
            // referenced column list, when present, to match the FK's own column count.
            if (ordered.All(r => r.To is not null))
                fk.ReferencedColumns.AddRange(ordered.Select(r => r.To!));

            foreignKeys.Add(fk);
        }

        table.ForeignKeys = foreignKeys;
    }

    private static async Task ReadUniqueConstraints(SqliteConnection cnxn, TableInfo table, CancellationToken ct)
    {
        var uniqueIndexes = new List<string>();

        await using (var listCmd = cnxn.CreateCommand())
        {
            listCmd.CommandText = $"PRAGMA index_list({Quote(table.Name)})";
            await using SqliteDataReader reader = await listCmd.ExecuteReaderAsync(ct);

            int nameOrdinal = reader.GetOrdinal("name");
            int uniqueOrdinal = reader.GetOrdinal("unique");
            int originOrdinal = reader.GetOrdinal("origin");

            while (await reader.ReadAsync(ct))
            {
                string indexName = reader.GetString(nameOrdinal);
                bool isUnique = reader.GetInt32(uniqueOrdinal) != 0;
                // "origin" is 'c' (CREATE INDEX), 'u' (UNIQUE column/table constraint), or 'pk'
                // (the autoindex SQLite creates to enforce a non-rowid-alias primary key). The
                // 'pk' autoindex is not a distinct constraint — it just duplicates PrimaryKey —
                // so it must not be reported as a UniqueConstraintInfo.
                string origin = reader.GetString(originOrdinal);
                if (isUnique && origin != "pk") uniqueIndexes.Add(indexName);
            }
        }

        foreach (string indexName in uniqueIndexes.OrderBy(n => n, StringComparer.Ordinal))
        {
            var constraint = new UniqueConstraintInfo { Name = indexName };

            await using var infoCmd = cnxn.CreateCommand();
            infoCmd.CommandText = $"PRAGMA index_info({Quote(indexName)})";

            await using SqliteDataReader reader = await infoCmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                if (!reader.IsDBNull(2)) constraint.Columns.Add(reader.GetString(2));

            if (constraint.Columns.Count > 0) table.UniqueConstraints.Add(constraint);
        }
    }

    /// <summary>
    /// A single-column INTEGER PRIMARY KEY is a rowid alias and auto-assigns, with or without the
    /// AUTOINCREMENT keyword — EXCEPT on a WITHOUT ROWID table, where there is no rowid to alias,
    /// so the column is an ordinary primary key that does not auto-assign. WITHOUT ROWID is not
    /// exposed by any PRAGMA, so the table's own DDL from sqlite_master is the only source for it.
    /// </summary>
    private static async Task MarkAutoIncrement(SqliteConnection cnxn, TableInfo table, CancellationToken ct)
    {
        if (table.Kind != TableKind.Table) return;

        string keyColumn = table.PrimaryKey[0];
        ColumnInfo? column = table.Columns.FirstOrDefault(c => c.Name == keyColumn);
        if (column is null || column.NativeType != "INTEGER") return;

        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name = $name";
        cmd.Parameters.AddWithValue("$name", table.Name);

        if (await cmd.ExecuteScalarAsync(ct) is not string ddl) return;

        column.IsIdentity = !WithoutRowidPattern.IsMatch(ddl);
    }

    private static readonly Regex WithoutRowidPattern =
        new("""\bWITHOUT\s+ROWID\b""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string Quote(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
