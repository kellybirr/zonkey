namespace Zonkey.Scaffold.Mapping;

/// <summary>
/// Shared logic across provider-specific <see cref="ITypeMapper"/> implementations. Extracted once
/// three providers (SQLite, PostgreSQL, MySQL) needed the identical nullability decoration, rather
/// than let a fourth (SQL Server) copy it again.
/// </summary>
internal static class TypeMapperSupport
{
    /// <summary>
    /// Applies nullable annotation to a bare CLR type name. Value types get "?" whenever the
    /// column is nullable; reference types (including "byte[]") only get "?" when nullable
    /// reference types are enabled for the generated output.
    /// </summary>
    public static string Decorate(string clr, bool isReference, bool isNullable, bool nullableRefs)
    {
        if (!isNullable) return clr;
        if (clr == "byte[]") return nullableRefs ? "byte[]?" : "byte[]";
        return isReference
            ? (nullableRefs ? clr + "?" : clr)
            : clr + "?";
    }
}
