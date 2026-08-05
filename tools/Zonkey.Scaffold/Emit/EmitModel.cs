namespace Zonkey.Scaffold.Emit;

public sealed class EntityModel
{
    public string ClassName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string? SchemaName { get; set; }
    public string? SaveToTable { get; set; }
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// The namespace this entity is declared in, already escaped — <c>null</c> to fall back to
    /// <see cref="EntityEmitOptions.Namespace"/>.
    /// </summary>
    /// <remarks>
    /// Per entity rather than per run because <c>--schema-disambiguation namespace</c> puts each
    /// schema's classes in a namespace of their own: the run has no single answer any more.
    /// <c>ScaffoldPipeline</c> sets it on every entity it builds — including the ordinary case,
    /// where it is just the run's namespace — so the value the emitter writes, the value the
    /// wrapper qualifies its adapters with, and the value <c>EmittedSurface</c> checks type-name
    /// uniqueness within are one value and not three derivations of it. The fallback exists for
    /// models built by hand (the emitter tests, and any caller that has only a run-wide namespace
    /// to give).
    /// </remarks>
    public string? Namespace { get; set; }

    public List<PropertyModel> Properties { get; set; } = new();

    /// <summary>
    /// In-memory graph members derived from foreign keys, emitted only with <c>Emit:Relations</c>.
    /// </summary>
    public List<RelationModel> Relations { get; set; } = new();
}

/// <summary>
/// A navigation member the adapter never sees. Zonkey has no navigation properties: a member
/// without <c>[DataField]</c> is invisible to the adapter, so these are somewhere to put related
/// rows you loaded yourself, not something that loads them.
/// </summary>
public sealed class RelationModel
{
    public string MemberName { get; set; } = "";

    /// <summary>The related entity's class name.</summary>
    public string TypeName { get; set; } = "";

    /// <summary>A list of children, rather than a single parent.</summary>
    public bool IsCollection { get; set; }

    /// <summary>The foreign key this came from, for the emitted comment.</summary>
    public string Origin { get; set; } = "";

    /// <summary>The property on the owning class whose value identifies the related rows.</summary>
    public string LocalKey { get; set; } = "";

    /// <summary>The property on <see cref="TypeName"/> that the query filters on.</summary>
    public string ForeignKey { get; set; } = "";

    /// <summary>The key's CLR type with any <c>?</c> removed — picks the SqlIn helper.</summary>
    public string KeyClrType { get; set; } = "";

    public bool LocalKeyIsNullable { get; set; }
    public bool ForeignKeyIsNullable { get; set; }

    /// <summary>
    /// False for a composite foreign key, which cannot be expressed as a single <c>IN</c>. The
    /// member is still emitted — it is only the generated loader that has to be left out.
    /// </summary>
    public bool CanLoad { get; set; }
}

public sealed class PropertyModel
{
    public string Name { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string DbType { get; set; } = "";
    public string ClrType { get; set; } = "";
    public bool IsKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsRowVersion { get; set; }
    public bool IsNullable { get; set; }
    public int? Length { get; set; }
    public string? DateTimeKind { get; set; }
    public string? SequenceName { get; set; }
}

/// <summary>
/// What the entity emitter needs to know beyond the model itself.
/// </summary>
/// <remarks>
/// A <c>record</c> rather than a class purely so <c>CSharpEntityEmitter</c>'s <c>field</c>-keyword
/// fallback can say <c>options with { FieldKeyword = false }</c>. It used to hand-copy all six
/// properties, so a seventh added here and not added there would be silently dropped — and only
/// for the classes that take the fallback, i.e. one file out of a run, with no golden baseline
/// moved and nothing to notice. Nothing compares instances or depends on reference identity, so
/// the value semantics a record brings are unobserved.
/// </remarks>
public sealed record EntityEmitOptions
{
    public string? Namespace { get; set; }
    public bool PartialClasses { get; set; } = true;
    public bool VirtualProperties { get; set; }
    public bool FieldKeyword { get; set; }
    public bool PrivateFieldsAtTop { get; set; }
    public bool NullableRefs { get; set; }
}
