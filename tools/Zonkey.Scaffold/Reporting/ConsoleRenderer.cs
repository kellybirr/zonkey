using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Pipeline;

namespace Zonkey.Scaffold.Reporting;

public static class ConsoleRenderer
{
    public static void RenderInspect(ScaffoldPlan plan, TextWriter output)
    {
        output.WriteLine($"Provider : {plan.Provider} {plan.ServerVersion}");
        output.WriteLine($"Schemas  : {string.Join(", ", plan.Schemas)}");
        output.WriteLine();

        foreach (var entity in plan.Entities.OrderBy(e => e.TableName, StringComparer.Ordinal))
        {
            string kind = entity.IsReadOnly ? " (read-only)" : "";
            output.WriteLine($"{entity.TableName} -> {entity.ClassName}{kind}");

            foreach (var p in entity.Properties)
            {
                string flags = string.Concat(
                    p.IsKey ? " [key]" : "",
                    p.IsIdentity ? " [identity]" : "",
                    p.IsNullable ? " [null]" : "");

                output.WriteLine($"    {p.ColumnName,-24} {p.ClrType,-16} DbType.{p.DbType}{flags}");
            }
            output.WriteLine();
        }

        RenderList(output, "Skipped", plan.Skipped
            .Select(s => $"{s.Table}{(s.Column is null ? "" : "." + s.Column)}  " +
                         $"({s.Reason}: {s.Pattern})"));

        RenderList(output, "Warnings", plan.Warnings
            .Where(w => w.Level != WarningLevel.Info)
            .Select(w => $"[{w.Code}] {w.Message}"));
    }

    private static void RenderList(TextWriter output, string title, IEnumerable<string> lines)
    {
        var materialized = lines.ToList();
        if (materialized.Count == 0) return;

        output.WriteLine($"{title}:");
        foreach (string line in materialized) output.WriteLine($"    {line}");
        output.WriteLine();
    }
}
