using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Pipeline;
using Zonkey.Scaffold.Reporting;

namespace Zonkey.Scaffold.Commands;

public sealed class FileReport
{
    public string Path { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Table { get; set; } = "";
}

public sealed class GenerateReport
{
    public List<FileReport> Files { get; set; } = new();
    public List<Diagnostics.ScaffoldWarning> Warnings { get; set; } = new();
    public List<Selection.SkipRecord> Skipped { get; set; } = new();
}

public static class GenerateCommand
{
    public static async Task<int> Run(
        ScaffoldOptions options, bool dryRun, bool json,
        string workingDirectory, TextWriter stdout, CancellationToken ct)
    {
        OutputLayout layout = OutputLayout.Resolve(options, workingDirectory);
        ProjectCapabilities capabilities = ProjectProbe.Probe(layout.EntitiesDirectory);

        ScaffoldPlan plan = await ScaffoldPipeline.Build(options, capabilities, layout, ct);

        var emitOptions = new EntityEmitOptions
        {
            // plan.Namespace, not options.Namespace: the plan's is escaped, and the entity files
            // must spell the namespace the same way the wrapper does or the wrapper cannot name
            // the entity types.
            Namespace = plan.Namespace,
            PartialClasses = options.Emit.PartialClasses,
            VirtualProperties = options.Emit.VirtualProperties,
            // From the plan, not re-resolved from `capabilities`: the pipeline refuses the
            // identifiers these two flags decide the existence of, so a second derivation here
            // could disagree with the one that was checked.
            FieldKeyword = plan.FieldKeyword,
            PrivateFieldsAtTop = options.Emit.PrivateFieldsAtTop,
            NullableRefs = plan.NullableRefs
        };

        var writer = new GeneratedFileWriter(dryRun);
        var entityEmitter = new CSharpEntityEmitter();
        var report = new GenerateReport { Warnings = plan.Warnings, Skipped = plan.Skipped };

        // Written straight from plan.Emitted — the same list, with the same resolved paths, that
        // the pipeline just refused every collision in. Recomputing the paths here is what let a
        // wrapper file land beside an entity file the check had been told it could not meet.
        foreach (EmittedType unit in plan.Emitted)
        {
            string source = unit.IsWrapper
                ? new CSharpWrapperEmitter().Emit(plan.Wrapper)
                : entityEmitter.Emit(unit.Entity!, emitOptions);

            WriteOutcome outcome = writer.Write(unit.FilePath, source);

            report.Files.Add(new FileReport
            {
                Path = unit.FilePath,
                Outcome = outcome.ToString(),
                Table = unit.IsWrapper ? "(wrapper)" : unit.Entity!.TableName
            });
        }

        if (json)
        {
            stdout.WriteLine(JsonOutput.Serialize(report, options.ConnectionString));
        }
        else
        {
            foreach (FileReport f in report.Files)
                stdout.WriteLine($"{f.Outcome,-12} {f.Path}");

            // Info-level entries are excluded, exactly as ConsoleRenderer excludes them from
            // `inspect`'s Warnings list. Two commands reporting different warning counts for one
            // plan is a contradiction in the tool's own output, and this is the wrong side of it:
            // `info` is what a warning is downgraded to once it has been *answered* (a dbType
            // override supplying the type the mapper could not infer), so counting them tells an
            // agent to act on something it has already fixed — the loop that downgrade exists to
            // break, surviving in the one number the summary reports. Nothing is hidden: the
            // --json payload above carries every warning, level and all.
            int warnings = plan.Warnings.Count(w => w.Level != Diagnostics.WarningLevel.Info);

            stdout.WriteLine();
            stdout.WriteLine($"{report.Files.Count} file(s), {warnings} warning(s), " +
                             $"{plan.Skipped.Count} skipped.");
        }

        return 0;
    }
}
