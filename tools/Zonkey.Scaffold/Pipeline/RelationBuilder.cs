using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Naming;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Pipeline;

/// <summary>
/// Turns foreign keys into in-memory graph members.
/// </summary>
/// <remarks>
/// Zonkey has no navigation properties and no lazy loading, and this does not add any. A member
/// without <c>[DataField]</c> is invisible to the adapter, so what is emitted here is somewhere to
/// put children you loaded with a second query — the pattern in docs/modeling-relationships.md.
/// The generated <c>Fill…For</c> extensions issue that second query explicitly; nothing loads by
/// itself.
/// </remarks>
public static class RelationBuilder
{
    public static void Apply(
        List<(TableInfo Table, EntityModel Entity)> built, Inflector inflector, ICollection<string> warnings)
    {
        Dictionary<string, EntityModel> byTable = built.ToDictionary(
            b => b.Table.QualifiedName, b => b.Entity, StringComparer.OrdinalIgnoreCase);

        foreach ((TableInfo table, EntityModel child) in built)
        {
            foreach (ForeignKeyInfo fk in table.ForeignKeys)
            {
                string parentKey = string.IsNullOrEmpty(fk.ReferencedSchema)
                    ? fk.ReferencedTable
                    : $"{fk.ReferencedSchema}.{fk.ReferencedTable}";

                // A foreign key into a table this run did not generate has no type to name.
                if (!byTable.TryGetValue(parentKey, out EntityModel? parent)) continue;

                // Only a single-column key can be expressed as one IN, which is what the generated
                // loader needs. The members are still worth emitting; the loader is not.
                bool single = fk.Columns.Count == 1 && fk.ReferencedColumns.Count == 1;

                PropertyModel? childKey = single ? Find(child, fk.Columns[0]) : null;
                PropertyModel? parentKeyProp = single ? Find(parent, fk.ReferencedColumns[0]) : null;
                bool loadable = childKey is not null && parentKeyProp is not null;

                if (single && !loadable)
                {
                    warnings.Add(
                        $"Foreign key on {table.QualifiedName} references a column that was not " +
                        "generated, so no Fill extension was emitted for it.");
                }

                string reference = ReferenceName(fk, parent.ClassName);
                string origin = $"{table.QualifiedName}.{string.Join(", ", fk.Columns)} -> {parentKey}";

                // The child's parent reference: query the parent, match on the child's FK value.
                Add(child, new RelationModel
                {
                    MemberName = reference,
                    TypeName = parent.ClassName,
                    IsCollection = false,
                    Origin = origin,
                    CanLoad = loadable,
                    LocalKey = childKey?.Name ?? "",
                    ForeignKey = parentKeyProp?.Name ?? "",
                    KeyClrType = Bare(parentKeyProp?.ClrType),
                    LocalKeyIsNullable = childKey?.IsNullable ?? false,
                    ForeignKeyIsNullable = parentKeyProp?.IsNullable ?? false,
                }, warnings);

                // The parent's child collection, named after the child and disambiguated by the
                // reference when one table points at another more than once.
                bool ambiguous = table.ForeignKeys.Count(f =>
                    string.Equals(f.ReferencedTable, fk.ReferencedTable, StringComparison.OrdinalIgnoreCase)) > 1;

                string collection = inflector.Pluralize(child.ClassName);
                if (ambiguous) collection = reference + collection;

                Add(parent, new RelationModel
                {
                    MemberName = collection,
                    TypeName = child.ClassName,
                    IsCollection = true,
                    Origin = origin,
                    CanLoad = loadable,
                    LocalKey = parentKeyProp?.Name ?? "",
                    ForeignKey = childKey?.Name ?? "",
                    KeyClrType = Bare(childKey?.ClrType),
                    LocalKeyIsNullable = parentKeyProp?.IsNullable ?? false,
                    ForeignKeyIsNullable = childKey?.IsNullable ?? false,
                }, warnings);
            }
        }
    }

    private static PropertyModel? Find(EntityModel entity, string columnName)
        => entity.Properties.FirstOrDefault(
            p => string.Equals(p.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Strips the nullable marker; the key's underlying type is what picks the helper.</summary>
    private static string Bare(string? clrType) => clrType?.TrimEnd('?') ?? "";

    /// <summary>
    /// Names the parent reference after the foreign key column with its <c>Id</c> suffix removed —
    /// <c>species_id</c> gives <c>Species</c> — which is what distinguishes two keys into the same
    /// table. Falls back to the parent's class name for a composite or oddly named key.
    /// </summary>
    private static string ReferenceName(ForeignKeyInfo fk, string parentClassName)
    {
        if (fk.Columns.Count != 1) return parentClassName;

        string stem = fk.Columns[0];

        foreach (string suffix in new[] { "_id", "id" })
        {
            if (stem.Length > suffix.Length && stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                stem = stem[..^suffix.Length].TrimEnd('_');
                break;
            }
        }

        string name = NamingEngine.ToPascalCase(stem);

        return string.IsNullOrEmpty(name) ? parentClassName : name;
    }

    /// <summary>
    /// Adds a member unless the name is already taken. A relation is a convenience; a mapped column
    /// is the data, so the column wins and the caller is told which member was dropped.
    /// </summary>
    private static void Add(EntityModel target, RelationModel relation, ICollection<string> warnings)
    {
        relation.MemberName = NamingEngine.EscapeKeyword(relation.MemberName);

        bool taken = string.Equals(relation.MemberName, target.ClassName, StringComparison.Ordinal)
            || target.Properties.Any(p => string.Equals(p.Name, relation.MemberName, StringComparison.Ordinal))
            || target.Relations.Any(r => string.Equals(r.MemberName, relation.MemberName, StringComparison.Ordinal));

        if (taken)
        {
            warnings.Add(
                $"Relation member '{target.ClassName}.{relation.MemberName}' (from {relation.Origin}) " +
                "collides with an existing member and was not emitted. Add it by hand if you want it.");
            return;
        }

        target.Relations.Add(relation);
    }
}
