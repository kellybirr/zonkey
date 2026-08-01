using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Schema;
using Zonkey.Scaffold.Selection;

public class SchemaFilterTests
{
    private static DatabaseSchema Sample() => new()
    {
        Provider = "sqlite",
        Tables =
        [
            Table("public", "orders",       TableKind.Table, "id", "tenant_id", "total"),
            Table("public", "__migrations", TableKind.Table, "id"),
            Table("public", "aspnet_users", TableKind.Table, "id"),
            Table("public", "order_view",   TableKind.View,  "id"),
            Table("other",  "orders",       TableKind.Table, "id"),
        ]
    };

    private static TableInfo Table(string schema, string name, TableKind kind, params string[] cols)
    {
        var t = new TableInfo { Schema = schema, Name = name, Kind = kind };
        for (int i = 0; i < cols.Length; i++)
            t.Columns.Add(new ColumnInfo { Name = cols[i], NativeType = "INTEGER", Ordinal = i });
        return t;
    }

    private static FilterResult Run(
        SelectionOptions? include = null, IgnoreOptions? ignore = null, EmitOptions? emit = null)
        => SchemaFilter.Apply(Sample(), ["public"], include ?? new(), ignore ?? new(), emit ?? new());

    [Fact]
    public void Schemas_outside_scope_are_removed()
    {
        var r = Run();
        Assert.DoesNotContain(r.Schema.Tables, t => t.Schema == "other");
    }

    [Fact]
    public void Views_are_excluded_by_default()
    {
        var r = Run();
        Assert.DoesNotContain(r.Schema.Tables, t => t.Kind == TableKind.View);
    }

    [Fact]
    public void Views_are_included_when_enabled()
    {
        var r = Run(emit: new EmitOptions { Views = true });
        Assert.Contains(r.Schema.Tables, t => t.Name == "order_view");
    }

    [Fact]
    public void Ignore_globs_remove_tables()
    {
        var r = Run(ignore: new IgnoreOptions { Tables = ["__*", "aspnet_*"] });
        Assert.DoesNotContain(r.Schema.Tables, t => t.Name == "__migrations");
        Assert.DoesNotContain(r.Schema.Tables, t => t.Name == "aspnet_users");
        Assert.Contains(r.Schema.Tables, t => t.Name == "orders");
    }

    [Fact]
    public void Include_when_present_restricts_first_then_ignore_wins()
    {
        var r = Run(
            include: new SelectionOptions { Tables = ["orders", "__migrations"] },
            ignore:  new IgnoreOptions   { Tables = ["__*"] });

        Assert.Single(r.Schema.Tables);
        Assert.Equal("orders", r.Schema.Tables[0].Name);
    }

    [Fact]
    public void Column_patterns_remove_columns()
    {
        var r = Run(ignore: new IgnoreOptions { Columns = ["*.tenant_id"] });
        var orders = r.Schema.Tables.Single(t => t.Name == "orders");
        Assert.DoesNotContain(orders.Columns, c => c.Name == "tenant_id");
        Assert.Contains(orders.Columns, c => c.Name == "total");
    }

    [Fact]
    public void Every_skip_is_attributed_to_its_pattern()
    {
        var r = Run(ignore: new IgnoreOptions { Tables = ["aspnet_*"], Columns = ["*.tenant_id"] });

        var table = r.Skipped.Single(s => s.Table == "public.aspnet_users" && s.Column is null);
        Assert.Equal("ignore.tables", table.Reason);
        Assert.Equal("aspnet_*", table.Pattern);

        var column = r.Skipped.Single(s => s.Column == "tenant_id");
        Assert.Equal("public.orders", column.Table);
        Assert.Equal("ignore.columns", column.Reason);
        Assert.Equal("*.tenant_id", column.Pattern);
    }

    [Fact]
    public void Qualified_patterns_match_schema_prefixed_names()
    {
        var r = Run(ignore: new IgnoreOptions { Tables = ["public.__*"] });
        Assert.DoesNotContain(r.Schema.Tables, t => t.Name == "__migrations");
    }

    [Fact]
    public void Skip_records_disambiguate_same_named_tables_in_different_schemas()
    {
        var r = SchemaFilter.Apply(
            Sample(), ["public", "other"],
            new SelectionOptions(),
            new IgnoreOptions { Tables = ["orders"] },
            new EmitOptions());

        var skippedOrderTables = r.Skipped.Where(s => s.Reason == "ignore.tables" && s.Column is null).ToList();
        Assert.Equal(2, skippedOrderTables.Count);
        Assert.Contains(skippedOrderTables, s => s.Table == "public.orders");
        Assert.Contains(skippedOrderTables, s => s.Table == "other.orders");
        Assert.NotEqual(skippedOrderTables[0].Table, skippedOrderTables[1].Table);
    }

    [Fact]
    public void Dotless_ignore_column_pattern_throws_naming_pattern_and_suggested_form()
    {
        var ex = Assert.Throws<ScaffoldException>(() =>
            Run(ignore: new IgnoreOptions { Columns = ["tenant_id"] }));

        Assert.Contains("tenant_id", ex.Message);
        Assert.Contains("*.tenant_id", ex.Message);
    }

    [Fact]
    public void Multiple_dotless_ignore_column_patterns_are_all_named_in_one_exception()
    {
        var ex = Assert.Throws<ScaffoldException>(() =>
            Run(ignore: new IgnoreOptions { Columns = ["tenant_id", "total"] }));

        Assert.Contains("tenant_id", ex.Message);
        Assert.Contains("total", ex.Message);
    }

    [Fact]
    public void Well_formed_ignore_column_pattern_still_works_alongside_validation()
    {
        // Regression guard: validation must not break the two tests that pin the core
        // guarantee of this task (Column_patterns_remove_columns and
        // Every_skip_is_attributed_to_its_pattern exercise this already; this test adds a
        // belt-and-suspenders check that a single well-formed pattern passes validation).
        var r = Run(ignore: new IgnoreOptions { Columns = ["*.tenant_id"] });
        var orders = r.Schema.Tables.Single(t => t.Name == "orders");
        Assert.DoesNotContain(orders.Columns, c => c.Name == "tenant_id");
    }

    [Fact]
    public void Empty_ignore_columns_list_does_not_throw()
    {
        var r = Run(ignore: new IgnoreOptions { Columns = [] });
        Assert.NotNull(r);
    }

    [Fact]
    public void Dot_only_and_leading_or_trailing_dot_patterns_are_rejected_like_dotless_ones()
    {
        var ex = Assert.Throws<ScaffoldException>(() =>
            Run(ignore: new IgnoreOptions { Columns = ["orders.", ".", ".tenant_id"] }));

        Assert.Contains("orders.", ex.Message);
        Assert.Contains(".tenant_id", ex.Message);
    }
}
