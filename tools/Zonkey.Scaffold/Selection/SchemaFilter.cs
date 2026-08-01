using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Selection;

/// <summary>
/// Four ordered stages: schemas, include.tables, ignore.tables, ignore.columns.
/// The order is fixed so "why was this table skipped?" has exactly one answer, and every
/// removal is recorded with the pattern that caused it — a glob is powerful enough to
/// quietly eat something you needed.
/// </summary>
public static class SchemaFilter
{
    public static FilterResult Apply(
        DatabaseSchema schema,
        IReadOnlyList<string> schemaScope,
        SelectionOptions include,
        IgnoreOptions ignore,
        EmitOptions emit)
    {
        ValidateColumnPatterns(ignore.Columns);

        var skipped = new List<SkipRecord>();
        var kept = new List<TableInfo>();

        foreach (TableInfo table in schema.Tables)
        {
            // 1. schema scope
            if (!schemaScope.Contains(table.Schema, StringComparer.OrdinalIgnoreCase))
            {
                skipped.Add(new SkipRecord
                {
                    Table = table.QualifiedName, Reason = "schemas", Pattern = table.Schema
                });
                continue;
            }

            // 1b. views (a kind filter, not a pattern filter)
            if (table.Kind == TableKind.View && !emit.Views)
            {
                skipped.Add(new SkipRecord
                {
                    Table = table.QualifiedName, Reason = "emit.views", Pattern = "views=false"
                });
                continue;
            }

            // 2. include.tables — empty means "all"
            if (include.Tables.Count > 0 && MatchPattern(include.Tables, table) is null)
            {
                skipped.Add(new SkipRecord
                {
                    Table = table.QualifiedName, Reason = "include.tables", Pattern = "(not listed)"
                });
                continue;
            }

            // 3. ignore.tables — always wins over include
            string? ignoredBy = MatchPattern(ignore.Tables, table);
            if (ignoredBy is not null)
            {
                skipped.Add(new SkipRecord
                {
                    Table = table.QualifiedName, Reason = "ignore.tables", Pattern = ignoredBy
                });
                continue;
            }

            // 4. ignore.columns
            var keptColumns = new List<ColumnInfo>();
            foreach (ColumnInfo column in table.Columns)
            {
                string? colPattern = MatchColumnPattern(ignore.Columns, table, column);
                if (colPattern is not null)
                {
                    skipped.Add(new SkipRecord
                    {
                        Table = table.QualifiedName, Column = column.Name,
                        Reason = "ignore.columns", Pattern = colPattern
                    });
                    continue;
                }
                keptColumns.Add(column);
            }

            table.Columns = keptColumns;
            kept.Add(table);
        }

        schema.Tables = kept;
        return new FilterResult { Schema = schema, Skipped = skipped };
    }

    /// <summary>Returns the first pattern matching either the bare or the qualified name.</summary>
    private static string? MatchPattern(List<string> patterns, TableInfo table)
        => patterns.FirstOrDefault(p =>
            GlobPattern.IsMatch(p, table.Name) || GlobPattern.IsMatch(p, table.QualifiedName));

    /// <summary>
    /// Rejects any ignore.columns pattern that is not in "table.column" form before any
    /// filtering happens. A dotless pattern like "tenant_id" (instead of "*.tenant_id") is not
    /// auto-expanded or warned about — it silently matches nothing, so the user believes a
    /// column is excluded when it is not. That is exactly the failure the skip-attribution
    /// design exists to prevent, so it is a hard error instead. The same reasoning applies to a
    /// pattern that is only a dot, or has an empty table/column half (a leading or trailing
    /// dot): each of those also can never match a real column, so they fail the same way.
    /// </summary>
    private static void ValidateColumnPatterns(IReadOnlyList<string> patterns)
    {
        List<string> malformed = patterns.Where(IsMalformedColumnPattern).ToList();
        if (malformed.Count == 0)
            return;

        string details = string.Join(
            "\n", malformed.Select(p => $"  - {DescribeInvalidColumnPattern(p)}"));

        throw new ScaffoldException(
            $"{malformed.Count} ignore.columns pattern(s) are not in table.column form:\n{details}");
    }

    private static bool IsMalformedColumnPattern(string pattern)
    {
        int dot = pattern.LastIndexOf('.');
        return dot < 0 || dot == 0 || dot == pattern.Length - 1;
    }

    private static string DescribeInvalidColumnPattern(string pattern)
    {
        int dot = pattern.LastIndexOf('.');

        if (dot < 0)
            return $"'{pattern}' is not in table.column form. Use '*.{pattern}' to ignore that " +
                   $"column in every table, or 'orders.{pattern}' for one table.";

        string table = pattern[..dot];
        string column = pattern[(dot + 1)..];

        if (table.Length == 0 && column.Length == 0)
            return $"'{pattern}' is not in table.column form — a pattern cannot be just a dot. " +
                   "Use '*.<column>' to ignore a column in every table, or '<table>.<column>' for one table.";

        if (table.Length == 0)
            return $"'{pattern}' is missing the table part before the dot. Use '*.{column}' to " +
                   $"ignore '{column}' in every table, or '<table>.{column}' for one table.";

        return $"'{pattern}' is missing the column part after the dot. Use '{table}.<column>' " +
               $"naming the column, or put '{table}' in ignore.tables to drop the whole table.";
    }

    /// <summary>Column patterns are "table.column"; either half may be a glob.</summary>
    private static string? MatchColumnPattern(List<string> patterns, TableInfo table, ColumnInfo column)
    {
        foreach (string pattern in patterns)
        {
            int dot = pattern.LastIndexOf('.');
            if (dot < 0) continue;

            string tablePart = pattern[..dot];
            string columnPart = pattern[(dot + 1)..];

            bool tableMatches =
                GlobPattern.IsMatch(tablePart, table.Name) ||
                GlobPattern.IsMatch(tablePart, table.QualifiedName);

            if (tableMatches && GlobPattern.IsMatch(columnPart, column.Name))
                return pattern;
        }
        return null;
    }
}
