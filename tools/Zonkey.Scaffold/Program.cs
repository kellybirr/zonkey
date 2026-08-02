using Zonkey.Scaffold.Config;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Pipeline;

namespace Zonkey.Scaffold;

public static class Program
{
    private const string Usage = """
        zonkey-scaffold — generates Zonkey data classes from a live database.

          zonkey-scaffold --provider pgsql --connection "<conn>" --namespace Acme.Data --out ./Data

        Common options (any ScaffoldOptions member works as --Section:Key value):
          -p, --provider        sqlite | pgsql | mysql | mssql
          -c, --connection      ADO.NET connection string
          -n, --namespace       namespace for the generated classes
          -o, --out             output directory (default: current)
              --schema          schema to read; repeat with --Schemas:1, or use a ;-list
              --wrapper-class   wrapper class name (default: AppDatabase)
              --dry-run         report what would be written, write nothing
              --IgnoreTables    ;-separated names, trailing * allowed
              --Views true      include views
              --Naming:Singularize false
              --Emit:FieldKeyword false

        Settings also load from zonkey.scaffold.json and ZONKEY_SCAFFOLD_* environment variables.

          zonkey-scaffold skill --install [--out <dir>]
                                installs the agent skill into .claude/skills/zonkey-scaffold/

        The output is a starting point: review it, rename what you like, and edit it in place.
        """;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine(Usage);
            return 0;
        }

        try
        {
            if (args[0] is "skill")
                return Skill(args);

            ScaffoldOptions options = ConfigurationLoader.Load(args, Directory.GetCurrentDirectory());
            ScaffoldPlan plan = await ScaffoldPipeline.Build(options, CancellationToken.None);

            Write(plan, options);
            return 0;
        }
        catch (ScaffoldException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex}");
            return 2;
        }
    }

    private static int Skill(string[] args)
    {
        if (!args.Contains("--install"))
            throw new ScaffoldException("Usage: zonkey-scaffold skill --install [--out <dir>]");

        int i = Array.IndexOf(args, "--out");
        string? target = i >= 0 && i + 1 < args.Length ? args[i + 1] : null;

        return SkillInstaller.Install(Directory.GetCurrentDirectory(), target);
    }

    private static void Write(ScaffoldPlan plan, ScaffoldOptions options)
    {
        string root = Path.GetFullPath(options.Output.Entities, Directory.GetCurrentDirectory());
        string wrapperDir = options.Output.Wrapper is null
            ? root
            : Path.GetFullPath(options.Output.Wrapper, Directory.GetCurrentDirectory());

        string suffix = options.Output.GeneratedSuffix ? ".g.cs" : ".cs";
        var writer = new GeneratedFileWriter(options.DryRun);
        var entityEmitter = new CSharpEntityEmitter();
        var emitOptions = new EntityEmitOptions
        {
            Namespace = plan.Namespace,
            PartialClasses = options.Emit.PartialClasses,
            VirtualProperties = options.Emit.VirtualProperties,
            FieldKeyword = options.Emit.FieldKeyword,
            PrivateFieldsAtTop = options.Emit.PrivateFieldsAtTop,
            NullableRefs = options.Emit.NullableRefs,
        };

        foreach (EntityModel entity in plan.Entities)
        {
            writer.Write(
                Path.Combine(root, entity.ClassName + suffix),
                entityEmitter.Emit(entity, emitOptions));
        }

        writer.Write(
            Path.Combine(wrapperDir, plan.Wrapper.ClassName + suffix),
            new CSharpWrapperEmitter().Emit(plan.Wrapper));

        foreach (string warning in plan.Warnings.Distinct())
            Console.Error.WriteLine($"warning: {warning}");

        Console.WriteLine(options.DryRun
            ? $"{plan.Entities.Count} entities + wrapper (dry run, nothing written)."
            : $"Wrote {plan.Entities.Count} entities + {plan.Wrapper.ClassName} to {root}.");
    }
}
