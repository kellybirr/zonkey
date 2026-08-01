using System.Collections.Concurrent;
using System.Reflection;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Pipeline;

/// <summary>What kind of member an emitter will declare. Purely descriptive: every kind shares
/// one declaration space, so the collision check does not branch on it.</summary>
public enum DeclaredKind
{
    Property,
    BackingField,
    AdapterProperty
}

/// <summary>
/// One identifier an emitter will declare inside a type.
/// </summary>
/// <param name="Name">The identifier as it will appear in the source, <c>@</c> escape included.</param>
/// <param name="Kind">Which emitter construct produces it.</param>
/// <param name="Origin">
/// The schema object it derives from — a column name, or an entity class name for the wrapper's
/// adapter properties. Two members of one name that share an origin are one fault reported at a
/// coarser level (two tables mapping to one class name give the wrapper two identical adapter
/// properties), so the origin is what tells a genuine collision from an echo of one.
/// </param>
/// <param name="Description">How to name this member in a refusal message.</param>
/// <param name="Remedy">
/// The sentence that tells the caller which config key renames this member. Built here rather than
/// where the message is, because this is the only place that still holds the bare table name and
/// the column name the override key is spelled with; by the time a rule is looking at a
/// <see cref="DeclaredName"/> it has only the finished identifier.
/// </param>
public sealed record DeclaredName(
    string Name, DeclaredKind Kind, string Origin, string Description, string Remedy);

/// <summary>
/// One type this run will emit: the name it will be declared with, the file it will be written to,
/// and every identifier it will declare.
/// </summary>
/// <remarks>
/// The entity model itself rides along so <c>generate</c> can write straight from this list: the
/// set that was checked and the set that is written are then the same objects, not two derivations
/// that have to agree.
/// </remarks>
public sealed class EmittedType
{
    /// <summary>The type name as it will be declared, <c>@</c> escape included.</summary>
    public required string ClassName { get; init; }

    /// <summary>The absolute path the file will be written to, from <see cref="OutputLayout"/>.</summary>
    public required string FilePath { get; init; }

    /// <summary>The qualified table name this came from, or <c>(wrapper)</c>.</summary>
    public required string Table { get; init; }

    /// <summary>How to name this type in a refusal message.</summary>
    public required string Owner { get; init; }

    /// <summary>The sentence that tells the caller which config key renames this type.</summary>
    public required string Remedy { get; init; }

    /// <summary>
    /// The type this one will be declared as deriving from — the source of every member it
    /// inherits, and so of every member name it can hide.
    /// </summary>
    /// <remarks>
    /// <see cref="object"/> for a read-only entity, which is emitted with no base type at all
    /// (see <c>CSharpEntityEmitter.ClassDeclaration</c>) and can therefore only collide with
    /// object's own members — not <c>DataClass</c>'s. Stated per type rather than assumed by the
    /// rule, because that difference is exactly the kind of per-class variation a run-wide
    /// assumption has already got wrong here twice.
    /// </remarks>
    public required Type BaseType { get; init; }

    /// <summary>
    /// Whether the emitted declaration actually writes <c>: {BaseType}</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="object"/> is how <see cref="BaseType"/> spells "no base clause at all" — a
    /// read-only entity inherits object's members without naming it — so this is the one place
    /// that convention is stated, rather than every rule that has to know it.
    /// </remarks>
    public bool DeclaresBaseType => BaseType != typeof(object);

    public bool IsWrapper { get; init; }

    /// <summary>The entity to emit, or <c>null</c> for the wrapper.</summary>
    public EntityModel? Entity { get; init; }

    public IReadOnlyList<DeclaredName> Members { get; init; } = [];
}

/// <summary>
/// Computes what a run will actually emit — every declared identifier and every resolved file
/// path — and refuses any of it that could not compile or could not survive being written.
/// </summary>
/// <remarks>
/// This type exists because the previous shape of this check kept being wrong in the same way.
/// It guessed at the emitted output from beside the emitter, using its own copy of the emitter's
/// rules: it re-derived backing-field names, it read a run-wide <c>fieldKeyword</c> flag that the
/// emitter overrides per class, and it decided whether two files competed by testing an option
/// string for emptiness rather than by comparing resolved paths. Each guess was corrected as it
/// was reported, and each correction left a sibling wrong.
/// <para>
/// So there is one computation (<see cref="Of"/>) that produces the final identifiers and the final
/// paths, using the emitter's own predicates (<see cref="CSharpEntityEmitter.DeclaresBackingFields"/>,
/// <see cref="CSharpEntityEmitter.BackingField"/>) and <see cref="OutputLayout"/>'s own paths, and
/// every check runs over <em>that</em>. A member family that is emitted is a member family that is
/// checked, because they are the same list.
/// </para>
/// <para>
/// The families, and the diagnostic each one produces if it is allowed through:
/// </para>
/// <list type="bullet">
/// <item>type name vs. type name — CS0101, plus the silent file overwrite. The wrapper is one of
/// the types, so "an entity named after the wrapper" is not a separate rule;</item>
/// <item>member vs. member inside one type — CS0102. One grouping covers property vs. property,
/// backing field vs. backing field, property vs. another property's backing field, and adapter
/// property vs. adapter property, because they are all declarations in one space and it never
/// mattered which two kinds happened to meet;</item>
/// <item>member vs. its own type's name — CS0542, "member names cannot be the same as their
/// enclosing type": a column named after its table, or a backing field derived from one;</item>
/// <item>file path vs. file path, case-insensitively — no diagnostic at all, which is what makes it
/// worth refusing: the second file silently replaces the first.</item>
/// </list>
/// <para>
/// Two further families are collisions with names the tool does not declare but does depend on,
/// and both are <em>warnings</em> rather than refusals — the files are written and the caller is
/// told what will not compile and which key fixes it (see <see cref="Warn"/> for why that is the
/// right level for each):
/// </para>
/// <list type="bullet">
/// <item>a member whose name matches one inherited from the base type — CS0108. The set comes from
/// reflection over the base type (<see cref="InheritedMemberNames"/>), not a transcribed list, and
/// the base type is <see cref="object"/> for a read-only entity, which is emitted with no base at
/// all;</item>
/// <item>a class named after a type the emitted source depends on. This is <em>two</em> warnings,
/// because the consequences are opposite: a class named after its own base type derives from
/// itself and does not compile (CS0146), while a class named after a type the source merely
/// references compiles perfectly and silently rebinds that reference to the generated class —
/// which is the more dangerous outcome, since a build failure stops the caller and a rebind ships.
/// Which one applies is read off the unit's own base-type slot, not decided against a second list.
/// The names come from the emitters' own <c>ReferencedTypeNames</c> plus the CLR types this run's
/// properties were actually built with (<see cref="ReferencedTypeNames"/>).</item>
/// </list>
/// <para>
/// Namespaces are not a declaration space (that is what they are for) and string literals are not
/// identifiers, so neither is checked.
/// </para>
/// </remarks>
public static class EmittedSurface
{
    /// <summary>
    /// The full emitted surface of a run: one <see cref="EmittedType"/> per entity, in the order
    /// they will be written, then the wrapper.
    /// </summary>
    public static IReadOnlyList<EmittedType> Of(
        IReadOnlyList<(TableInfo Table, EntityModel Entity)> entities,
        WrapperModel wrapper,
        string wrapperClassName,
        bool fieldKeyword,
        OutputLayout layout)
    {
        var units = new List<EmittedType>(entities.Count + 1);

        foreach ((TableInfo table, EntityModel entity) in entities)
            units.Add(ForEntity(table, entity, fieldKeyword, layout));

        units.Add(ForWrapper(wrapper, wrapperClassName, layout, units));

        return units;
    }

    private static EmittedType ForEntity(
        TableInfo table, EntityModel entity, bool fieldKeyword, OutputLayout layout)
    {
        var members = new List<DeclaredName>();

        string ColumnRemedy(string column) =>
            $"Set overrides.tables.{table.Name}.columns.{column}.property to name the property " +
            "something else.";

        foreach (PropertyModel p in entity.Properties)
            members.Add(new DeclaredName(
                p.Name, DeclaredKind.Property, p.ColumnName, $"column '{p.ColumnName}'",
                ColumnRemedy(p.ColumnName)));

        if (CSharpEntityEmitter.DeclaresBackingFields(entity, fieldKeyword))
        {
            // A name that is nothing but the '@' escape has no backing field to derive;
            // NamingEngine refuses it earlier, and BackingField throws rather than index into it.
            foreach (PropertyModel p in entity.Properties.Where(p => p.Name.TrimStart('@').Length > 0))
                members.Add(new DeclaredName(
                    CSharpEntityEmitter.BackingField(p), DeclaredKind.BackingField, p.ColumnName,
                    $"property '{p.Name}' (column '{p.ColumnName}')",
                    ColumnRemedy(p.ColumnName)));
        }

        return new EmittedType
        {
            ClassName = entity.ClassName,
            FilePath = layout.EntityPath(entity.ClassName),
            Table = table.QualifiedName,
            Owner = $"Class '{entity.ClassName}' (table '{table.QualifiedName}')",
            Remedy = $"Set overrides.tables.{table.Name}.className to name the class something else.",
            // A read-only class is emitted without ": DataClass", so object is all it inherits.
            BaseType = entity.IsReadOnly ? typeof(object) : typeof(Zonkey.ObjectModel.DataClass),
            Entity = entity,
            Members = members
        };
    }

    private static EmittedType ForWrapper(
        WrapperModel wrapper, string className, OutputLayout layout,
        IReadOnlyList<EmittedType> entities)
    {
        EmittedType? UnitOf(string entityClassName) => entities.FirstOrDefault(
            u => string.Equals(u.ClassName, entityClassName, StringComparison.Ordinal));

        return new EmittedType
        {
            ClassName = className,
            FilePath = layout.WrapperPath(className),
            Table = "(wrapper)",
            Owner = $"The DatabaseWrapper class '{className}'",
            Remedy = "Pass --wrapper-class (wrapper.className) to name it something else.",
            BaseType = typeof(Zonkey.ObjectModel.DatabaseWrapper),
            IsWrapper = true,
            // An adapter property is named after its entity class, so the key that renames it is
            // the entity's className override, not anything spelled on the wrapper.
            Members = [.. wrapper.Entries.Select(e => new DeclaredName(
                e.PropertyName, DeclaredKind.AdapterProperty, e.EntityClassName,
                $"class '{e.EntityClassName}' (table '{UnitOf(e.EntityClassName)?.Table ?? e.EntityClassName}')",
                UnitOf(e.EntityClassName)?.Remedy ?? "Rename the class it is derived from."))]
        };
    }

    /// <summary>
    /// Refuses every collision in the computed surface, gathering all of them into one message.
    /// </summary>
    /// <remarks>
    /// One message rather than one per run: a caller fixing overrides should see the whole list
    /// once rather than discover it an error at a time. Each entry is also recorded as an
    /// error-level warning, and then this throws — nothing downstream inspects warning levels, so a
    /// warning alone would not stop <c>generate</c> writing the very files that cannot compile.
    /// </remarks>
    public static void Check(IReadOnlyList<EmittedType> units, ScaffoldPlan plan)
    {
        // Warnings first, and unconditionally: they are recorded whether or not a refusal follows,
        // and a caller who fixes a refusal should not have to re-run to discover these as well.
        Warn(units, plan);

        var found = new List<(string Table, string Message)>();

        // ---- type names, which is also where the wrapper meets the entities ----
        foreach (var g in units
                     .GroupBy(u => u.ClassName, StringComparer.Ordinal)
                     .Where(g => g.Count() > 1))
        {
            found.Add((g.First().Table, DescribeTypes(g)));
        }

        // ---- file paths: distinct names, one file ----
        // Grouped case-insensitively and reported only when the names really do differ, so an
        // exact-path clash (which is a type-name clash, already named above) is not said twice.
        foreach (var g in units
                     .GroupBy(u => u.FilePath, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Select(u => u.FilePath)
                                  .Distinct(StringComparer.Ordinal).Count() > 1))
        {
            string names = string.Join(", ", g
                .Select(u => u.ClassName)
                .Distinct(StringComparer.Ordinal)
                .Select(n => $"'{n}'"));

            found.Add((g.First().Table,
                $"Classes {names} differ only in case, so they are written to one file on a " +
                "case-insensitive filesystem (Windows and macOS) and one silently overwrites the " +
                "other. This is refused on every platform, including case-sensitive ones where " +
                "both files can coexist and both compile: generated code is committed and shared, " +
                "and output that is correct on one developer's machine while destroying a file on " +
                "another's is worse than an error both of them see."));
        }

        // ---- everything one type declares ----
        foreach (EmittedType unit in units)
        {
            // Members sharing a name *and* an origin are one fault seen twice — two tables mapping
            // to one class name give the wrapper the same adapter property twice — and the type
            // name check above already names the tables involved.
            foreach (var g in unit.Members
                         .GroupBy(m => m.Name, StringComparer.Ordinal)
                         .Where(g => g.Select(m => m.Origin)
                                      .Distinct(StringComparer.Ordinal).Count() > 1))
            {
                found.Add((unit.Table,
                    $"{unit.Owner}: {Enumerate(g.Select(m => m.Description))} " +
                    $"{(g.Count() > 2 ? "all" : "both")} declare the member '{g.Key}'."));
            }

            foreach (DeclaredName m in unit.Members
                         .Where(m => string.Equals(m.Name, unit.ClassName, StringComparison.Ordinal)))
            {
                found.Add((unit.Table,
                    $"{unit.Owner}: {m.Description} declares the member '{m.Name}', which is also " +
                    "the type name — a C# member may not be named after its enclosing type."));
            }
        }

        if (found.Count == 0) return;

        foreach ((string table, string message) in found)
        {
            plan.Warnings.Add(ScaffoldWarning.For(
                WarningCode.ClassNameCollision, message,
                table: table, level: WarningLevel.Error));
        }

        string details = string.Join("\n", found.Select(f => "  - " + f.Message));

        throw new ScaffoldException(
            $"{found.Count} name collision(s) found:\n{details}\n" +
            "Set overrides.tables.<table>.className to rename one of the colliding classes, " +
            "overrides.tables.<table>.columns.<column>.property to rename a property, " +
            "or --wrapper-class to rename the DatabaseWrapper.");
    }

    /// <summary>
    /// Records the two collisions with names the tool does not declare but does depend on. Both
    /// are warnings: generation proceeds and the files are written.
    /// </summary>
    /// <remarks>
    /// Warnings rather than refusals because the tool is not the authority on either outcome. The
    /// first (CS0108) is a compiler <em>warning</em> in an ordinary build — the emitted code is
    /// legal C# and does what it says; it only fails a build that has opted into
    /// <c>TreatWarningsAsErrors</c>, and a caller who has not may reasonably want the property
    /// named after their column. The second does not compile, but the caller can see that
    /// immediately, and refusing would leave them with no output at all for a fault an override
    /// key fixes in one line. So both name the table, the member, what breaks, and the key.
    /// <para>
    /// Both run over the same <see cref="EmittedType.Members"/> list every other rule uses, for the
    /// reason stated on this type: an identifier family that is emitted is one that is checked,
    /// because they are the same list. Neither adds a pass over the schema.
    /// </para>
    /// </remarks>
    private static void Warn(IReadOnlyList<EmittedType> units, ScaffoldPlan plan)
    {
        IReadOnlySet<string> referenced = ReferencedTypeNames(units);

        foreach (EmittedType unit in units)
        {
            // ---- CS0108: a declared member hides one inherited from the base type ----
            IReadOnlySet<string> inherited = InheritedMemberNames(unit.BaseType);

            foreach (DeclaredName m in unit.Members.Where(m => inherited.Contains(m.Name)))
            {
                plan.Warnings.Add(ScaffoldWarning.For(
                    WarningCode.HidesBaseMember,
                    // "through", not "on": object's members are inherited via DataClass, so a
                    // column named ToString hides one the base type did not itself declare.
                    $"{unit.Owner}: {m.Description} declares the member '{m.Name}', which hides " +
                    $"a member of the same name inherited through {unit.BaseType.Name}. The " +
                    "generated file raises CS0108 — a warning in an ordinary build, an error in " +
                    $"a project with TreatWarningsAsErrors. {m.Remedy}",
                    table: unit.Table, column: unit.IsWrapper ? null : m.Origin));
            }

            // ---- the class name captures a type the emitted source depends on ----
            //
            // Split by actual consequence, and derived rather than classified against a second
            // list: a type derives from itself only when the name it captured is the one in its
            // own base-type slot, which this unit already carries. Everything else the source
            // merely mentions, and mentioning is not deriving.
            if (unit.DeclaresBaseType &&
                string.Equals(unit.ClassName, unit.BaseType.Name, StringComparison.Ordinal))
            {
                plan.Warnings.Add(ScaffoldWarning.For(
                    WarningCode.ShadowsBaseType,
                    $"{unit.Owner}: the class is named after the type it derives from, so it " +
                    $"derives from itself — '{unit.ClassName} : {unit.BaseType.Name}' resolves to " +
                    "this same class. The generated code will not compile (CS0146, circular base " +
                    $"class dependency). {unit.Remedy}",
                    table: unit.Table));
            }
            else if (referenced.Contains(unit.ClassName))
            {
                plan.Warnings.Add(ScaffoldWarning.For(
                    WarningCode.ShadowsReferencedType,
                    $"{unit.Owner}: the generated source also references a type named " +
                    $"'{unit.ClassName}', and inside the generated namespace that reference now " +
                    "resolves to this class instead of the intended one. The generated code still " +
                    "compiles — this is not a build failure but a silent change of meaning, which " +
                    "is the more dangerous of the two: the reference binds to the generated class " +
                    $"and behaves as that class at run time. {unit.Remedy}",
                    table: unit.Table));
            }
        }
    }

    /// <summary>
    /// Every member name a class deriving from <paramref name="baseType"/> would hide by
    /// declaring it — the base type's own accessible members and every one it inherits, up to and
    /// including <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// Reflected off the type rather than written down. A literal list is a copy of another
    /// assembly's surface with nothing to keep it current: the day a member is added to
    /// <c>DataClass</c>, a list stops covering it, and the symptom is that the tool falls silent —
    /// no test fails, no build breaks, the warning simply is not raised. That is why the tool
    /// carries a reference to <c>Zonkey.Data</c> at all. The list it would replace is short enough
    /// to be tempting and exactly the kind of thing three previous fix waves here got wrong by
    /// transcribing instead of deriving.
    /// <para>
    /// The trade is real and worth stating: this reflects the <c>Zonkey.Data</c> the <em>tool</em>
    /// was built against, which is a different assembly from the one the caller's project
    /// references. A list would have had the same skew and no way to notice it; this at least
    /// tracks the repository automatically.
    /// </para>
    /// <para>
    /// Walked base class by base class with <see cref="BindingFlags.DeclaredOnly"/>, because
    /// <c>GetMembers</c> on a derived type does not return non-public inherited members and would
    /// miss <c>object.MemberwiseClone</c> and <c>object.Finalize</c>. Filtered to what a derived
    /// class can actually see: hiding is only diagnosed for a member the deriving class inherits,
    /// so <c>DataClass</c>'s private <c>_originalValues</c> is not in the set — which matters,
    /// because that is precisely the backing-field name a column called <c>OriginalValues</c>
    /// produces, and including it would make the rule fire on a correct program. Constructors and
    /// compiler-generated accessors are excluded for the same reason they are excluded from the
    /// declaration-space rules: nobody declares them.
    /// </para>
    /// </remarks>
    public static IReadOnlySet<string> InheritedMemberNames(Type baseType)
        => _inherited.GetOrAdd(baseType, static type =>
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            for (Type? t = type; t is not null; t = t.BaseType)
            {
                foreach (MemberInfo m in t.GetMembers(
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.Static |
                             BindingFlags.DeclaredOnly))
                {
                    if (VisibleToDerivedClass(m)) names.Add(m.Name);
                }
            }

            return names;
        });

    private static readonly ConcurrentDictionary<Type, IReadOnlySet<string>> _inherited = new();

    /// <summary>
    /// Whether a class deriving from the declaring type inherits this member, and so whether
    /// redeclaring its name hides anything.
    /// </summary>
    private static bool VisibleToDerivedClass(MemberInfo member) => member switch
    {
        ConstructorInfo => false,
        MethodInfo m => !m.IsSpecialName && Visible(m),
        FieldInfo f => !f.IsSpecialName && (f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly),
        PropertyInfo p => p.GetAccessors(nonPublic: true).Any(Visible),
        EventInfo e => e.GetAddMethod(nonPublic: true) is { } a && Visible(a),
        Type t => t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamORAssem,
        _ => false
    };

    private static bool Visible(MethodBase m) => m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly;

    /// <summary>
    /// Every simple type name the emitted source refers to, and which a generated class therefore
    /// must not be named after.
    /// </summary>
    /// <remarks>
    /// Two sources, and neither is a guess about what the emitters write. The verbatim names come
    /// from the emitters themselves
    /// (<see cref="CSharpEntityEmitter.ReferencedTypeNames"/>,
    /// <see cref="CSharpWrapperEmitter.ReferencedTypeNames"/>), declared beside the code that
    /// writes them; the rest are read off the properties this run actually built, so whatever a
    /// provider's type mapper produced — <c>DateTime</c>, <c>Guid</c>, a type a mapper added
    /// later — is covered without anyone listing it here.
    /// <para>
    /// The entity class names are deliberately absent. The wrapper names every entity type, but
    /// those references resolve to the classes this run emits, which is correct; a generated class
    /// named after <em>another</em> generated class is a duplicate type name and is refused by the
    /// type-name rule, not warned about here.
    /// </para>
    /// <para>
    /// This is a name the source references <em>somewhere in the run</em>, not necessarily in the
    /// file that would capture it, so it can over-warn at the margin: a run of nothing but
    /// read-only entities emits no <c>: DataClass</c> at all, yet a class named <c>DataClass</c>
    /// would still be reported (as a rebind, correctly not as CS0146 — the split is per unit, so
    /// it degrades to the milder message rather than to a false claim of a build failure). At
    /// warning level, on a name that is at best deeply confusing to have in a generated namespace,
    /// that is the side to err on.
    /// </para>
    /// </remarks>
    public static IReadOnlySet<string> ReferencedTypeNames(IReadOnlyList<EmittedType> units)
    {
        var names = new HashSet<string>(
            CSharpEntityEmitter.ReferencedTypeNames, StringComparer.Ordinal);

        names.UnionWith(CSharpWrapperEmitter.ReferencedTypeNames);

        foreach (EmittedType unit in units)
        {
            if (unit.Entity is null) continue;

            foreach (PropertyModel p in unit.Entity.Properties)
                if (SimpleTypeName(p.ClrType) is { } name) names.Add(name);
        }

        return names;
    }

    /// <summary>
    /// The bindable name in a CLR type as the emitter spells it: <c>byte[]?</c> gives
    /// <c>byte</c>, <c>DateTime?</c> gives <c>DateTime</c>. Returns <c>null</c> for anything that
    /// is not a plain name, which is all this rule can compare a class name against.
    /// </summary>
    private static string? SimpleTypeName(string clrType)
    {
        string name = clrType.TrimEnd('?');

        while (name.EndsWith("[]", StringComparison.Ordinal))
            name = name[..^2].TrimEnd('?');

        return name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c == '_') ? name : null;
    }

    private static string DescribeTypes(IGrouping<string, EmittedType> group)
    {
        List<EmittedType> fromTables = [.. group.Where(u => !u.IsWrapper)];

        string tables = string.Join(", ", fromTables.Select(u => $"'{u.Table}'"));
        string subject = fromTables.Count > 1 ? $"Tables {tables} all map" : $"Table {tables} maps";

        string wrapper = group.Any(u => u.IsWrapper)
            ? ", which is also the DatabaseWrapper class name"
            : "";

        return $"{subject} to class '{group.Key}'{wrapper}.";
    }

    /// <summary>"a", "a and b", "a, b and c".</summary>
    private static string Enumerate(IEnumerable<string> items)
    {
        List<string> all = [.. items];

        return all.Count switch
        {
            0 => "",
            1 => all[0],
            _ => string.Join(", ", all.Take(all.Count - 1)) + " and " + all[^1]
        };
    }
}
