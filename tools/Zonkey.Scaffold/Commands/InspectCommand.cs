using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Pipeline;
using Zonkey.Scaffold.Reporting;

namespace Zonkey.Scaffold.Commands;

public static class InspectCommand
{
    public static async Task<int> Run(
        ScaffoldOptions options, bool json, string workingDirectory,
        TextWriter stdout, CancellationToken ct)
    {
        // `inspect` writes nothing, but it resolves the same layout `generate` will: the plan it
        // previews includes the refusal of two entities that would compete for one file, and that
        // question is only answerable from resolved paths.
        OutputLayout layout = OutputLayout.Resolve(options, workingDirectory);

        ProjectCapabilities capabilities = ProjectProbe.Probe(layout.EntitiesDirectory);

        ScaffoldPlan plan = await ScaffoldPipeline.Build(options, capabilities, layout, ct);

        if (json) stdout.WriteLine(JsonOutput.Serialize(plan, options.ConnectionString));
        else ConsoleRenderer.RenderInspect(plan, stdout);

        return 0;
    }
}
