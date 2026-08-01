using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Selection;

public class SchemaScopeResolverTests
{
    [Fact]
    public void Explicit_request_is_honoured()
    {
        var r = SchemaScopeResolver.Resolve(["store"], ["store", "archive", "public"]);
        Assert.Equal(["store"], r);
    }

    [Fact]
    public void Single_available_schema_resolves_silently()
    {
        var r = SchemaScopeResolver.Resolve([], ["public"]);
        Assert.Equal(["public"], r);
    }

    [Fact]
    public void Multiple_available_schemas_without_request_is_an_error()
    {
        var ex = Assert.Throws<ScaffoldException>(
            () => SchemaScopeResolver.Resolve([], ["store", "archive"]));

        Assert.Contains("store", ex.Message);
        Assert.Contains("archive", ex.Message);
        Assert.Contains("--schema", ex.Message);   // the error must name its own remedy
    }

    [Fact]
    public void Star_selects_all_available()
    {
        var r = SchemaScopeResolver.Resolve(["*"], ["store", "archive"]);
        Assert.Equal(["store", "archive"], r);
    }

    [Fact]
    public void Requesting_unknown_schema_is_an_error_naming_what_exists()
    {
        var ex = Assert.Throws<ScaffoldException>(
            () => SchemaScopeResolver.Resolve(["nope"], ["public"]));
        Assert.Contains("nope", ex.Message);
        Assert.Contains("public", ex.Message);
    }

    [Fact]
    public void No_schemas_at_all_is_an_error()
        => Assert.Throws<ScaffoldException>(() => SchemaScopeResolver.Resolve([], []));

    [Fact]
    public void Request_matching_is_case_insensitive()
    {
        var r = SchemaScopeResolver.Resolve(["STORE"], ["store"]);
        Assert.Equal(["store"], r);     // returns the database's own casing
    }
}
