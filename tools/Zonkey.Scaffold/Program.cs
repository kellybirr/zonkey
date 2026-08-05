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
              --Language        CSharp (default) | VB
              --IgnoreTables    ;-separated names, trailing * allowed
              --Views true      include views
              --Naming:Singularize false
              --Emit:FieldKeyword false
              --Emit:Relations true   in-memory graph members for foreign keys

        Settings also load from zonkey.scaffold.json and ZONKEY_SCAFFOLD_* environment variables.

          zonkey-scaffold skill --install [--out <dir>]
                                writes the agent skill to .claude/skills/zonkey-scaffold/SKILL.md,
                                or to <dir>/SKILL.md when --out is given

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

    private static bool Is(string value, params string[] candidates)
        => candidates.Any(c => value.Equals(c, StringComparison.OrdinalIgnoreCase));

    private static void Write(ScaffoldPlan plan, ScaffoldOptions options)
    {
        string root = Path.GetFullPath(options.Output.Entities, Directory.GetCurrentDirectory());
        string wrapperDir = options.Output.Wrapper is null
            ? root
            : Path.GetFullPath(options.Output.Wrapper, Directory.GetCurrentDirectory());

        string language = options.Language.Trim();

        bool vb = Is(language, "vb", "visualbasic");
        if (!vb && !Is(language, "csharp", "cs"))
            throw new ScaffoldException($"Language = '{language}' is not a language. Use CSharp or VB.");

        string ext = vb ? ".vb" : ".cs";
        string suffix = options.Output.GeneratedSuffix ? ".g" + ext : ext;
        var writer = new GeneratedFileWriter(options.DryRun);
        var emitOptions = new EntityEmitOptions
        {
            Namespace = plan.Namespace,
            PartialClasses = options.Emit.PartialClasses,
            VirtualProperties = options.Emit.VirtualProperties,
            FieldKeyword = options.Emit.FieldKeyword,
            PrivateFieldsAtTop = options.Emit.PrivateFieldsAtTop,
            NullableRefs = options.Emit.NullableRefs,
        };

        var csharp = new CSharpEntityEmitter();
        var basic = new VbEntityEmitter();

        foreach (EntityModel entity in plan.Entities)
        {
            writer.Write(
                Path.Combine(root, entity.ClassName + suffix),
                vb ? basic.Emit(entity, emitOptions) : csharp.Emit(entity, emitOptions));
        }

        int extensions = 0;

        if (options.Emit.Relations && !vb)
        {
            var relations = new CSharpRelationsEmitter();

            foreach ((string typeName, List<RelationLoader> loaders)
                     in CSharpRelationsEmitter.Group(plan.Entities))
            {
                writer.Write(
                    Path.Combine(root, typeName + "Extensions" + suffix),
                    relations.Emit(typeName, loaders, emitOptions));

                extensions++;
            }
        }
        else if (options.Emit.Relations && vb)
        {
            Console.Error.WriteLine(
                "warning: Fill extensions are emitted for C# only; the VB relation members were " +
                "written without them.");
        }

        writer.Write(
            Path.Combine(wrapperDir, plan.Wrapper.ClassName + suffix),
            vb ? new VbWrapperEmitter().Emit(plan.Wrapper)
               : new CSharpWrapperEmitter().Emit(plan.Wrapper));

        foreach (string warning in plan.Warnings.Distinct())
            Console.Error.WriteLine($"warning: {warning}");

        string extra = extensions > 0 ? $" + {extensions} relation extension classes" : "";

        Console.WriteLine(options.DryRun
            ? $"{plan.Entities.Count} entities{extra} + wrapper (dry run, nothing written)."
            : $"Wrote {plan.Entities.Count} entities{extra} + {plan.Wrapper.ClassName} to {root}.");
    }
}
