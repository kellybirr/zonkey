using Xunit;
using Zonkey.Scaffold.Schema;

public class SchemaModelTests
{
    [Fact]
    public void QualifiedName_includes_schema_when_present()
    {
        var t = new TableInfo { Schema = "store", Name = "orders" };
        Assert.Equal("store.orders", t.QualifiedName);
    }

    [Fact]
    public void QualifiedName_is_bare_when_schema_empty()
    {
        var t = new TableInfo { Schema = "", Name = "orders" };
        Assert.Equal("orders", t.QualifiedName);
    }

    [Fact]
    public void Columns_default_to_empty_not_null()
    {
        var t = new TableInfo { Name = "x" };
        Assert.Empty(t.Columns);
        Assert.Empty(t.PrimaryKey);
        Assert.Empty(t.ForeignKeys);
    }

    [Fact]
    public void Table_without_primary_key_is_detectable()
    {
        var t = new TableInfo { Name = "audit_log" };
        Assert.False(t.HasPrimaryKey);
    }
}
