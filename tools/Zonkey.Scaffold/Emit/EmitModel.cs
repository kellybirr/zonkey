namespace Zonkey.Scaffold.Emit;

public sealed class EntityModel
{
    public string ClassName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string? SchemaName { get; set; }
    public string? SaveToTable { get; set; }
    public bool IsReadOnly { get; set; }
    public List<PropertyModel> Properties { get; set; } = new();
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
