using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Naming;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Schema;

public class NamingEngineTests
{
    private static TableInfo T(string name) => new() { Name = name };
    private static ColumnInfo C(string name) => new() { Name = name };

    private static NamingEngine Engine(NamingOptions? n = null, OverrideOptions? o = null)
        => new(n ?? new NamingOptions(), o ?? new OverrideOptions());

    private static string Class(string table, NamingOptions? n = null, OverrideOptions? o = null)
        => Engine(n, o).ClassNameFor(T(table), new List<ScaffoldWarning>());

    [Theory]
    [InlineData("orders", "Order")]
    [InlineData("order_lines", "OrderLine")]
    [InlineData("Species", "Species")]
    [InlineData("FeedingSchedule", "FeedingSchedule")]
    [InlineData("customer_addresses", "CustomerAddress")]
    public void Class_names_are_pascal_and_singular(string table, string expected)
        => Assert.Equal(expected, Class(table));

    [Fact]
    public void Preserve_style_skips_case_conversion()
        => Assert.Equal("order_line",
            Class("order_lines", new NamingOptions { Style = "preserve" }));

    [Fact]
    public void Singularize_can_be_turned_off()
        => Assert.Equal("Orders", Class("orders", new NamingOptions { Singularize = false }));

    [Fact]
    public void Prefix_and_suffix_are_applied()
        => Assert.Equal("DbOrderEntity",
            Class("orders", new NamingOptions { ClassPrefix = "Db", ClassSuffix = "Entity" }));

    [Fact]
    public void Table_override_wins_over_everything()
    {
        var o = new OverrideOptions();
        o.Tables["orders"] = new TableOverride { ClassName = "SalesOrder" };
        Assert.Equal("SalesOrder", Class("orders", null, o));
    }

    [Fact]
    public void Uncertain_inflection_raises_a_warning()
    {
        var warnings = new List<ScaffoldWarning>();
        string name = Engine().ClassNameFor(T("people"), warnings);

        Assert.Equal("Person", name);
        Assert.Contains(warnings, w => w.Code == WarningCode.InflectionUncertain);
    }

    [Fact]
    public void Obvious_inflection_raises_no_warning()
    {
        var warnings = new List<ScaffoldWarning>();
        Engine().ClassNameFor(T("animals"), warnings);
        Assert.Empty(warnings);
    }

    [Theory]
    [InlineData("customer_id", "CustomerId")]
    [InlineData("CustomerID", "CustomerID")]
    [InlineData("is_open", "IsOpen")]
    public void Property_names_are_pascal(string column, string expected)
        => Assert.Equal(expected, Engine().PropertyNameFor(T("orders"), C(column), "Order"));

    [Fact]
    public void StripClassName_is_off_by_default()
        => Assert.Equal("OrderDate",
            Engine().PropertyNameFor(T("orders"), C("order_date"), "Order"));

    [Fact]
    public void StripClassName_removes_the_class_prefix_when_enabled()
    {
        var e = Engine(new NamingOptions { StripClassName = true });
        Assert.Equal("Date", e.PropertyNameFor(T("orders"), C("order_date"), "Order"));
    }

    [Fact]
    public void StripClassName_never_empties_a_name()
    {
        var e = Engine(new NamingOptions { StripClassName = true });
        Assert.Equal("Order", e.PropertyNameFor(T("orders"), C("order"), "Order"));
    }

    [Fact]
    public void Column_override_wins()
    {
        var o = new OverrideOptions();
        o.Tables["orders"] = new TableOverride
        {
            Columns = { ["customer_id"] = new ColumnOverride { Property = "BuyerId" } }
        };
        Assert.Equal("BuyerId", Engine(null, o).PropertyNameFor(T("orders"), C("customer_id"), "Order"));
    }

    // ---- C# keyword identifiers ------------------------------------------------------
    //
    // A table or column named `event`, `class`, `lock` or `default` produced non-compiling
    // output. PascalCasing hides most single-word cases by accident (`class` -> `Class`), which
    // is exactly why this survived: the cases it does not hide are --naming-style preserve, and
    // lower-case-with-underscores schemas where `event` and `order` are ordinary table names.
    // Escaping belongs here rather than in the emitters, so every emitter (including the VB one
    // that does not exist yet) gets a valid identifier handed to it.

    private static NamingOptions Preserve() => new() { Style = "preserve", Singularize = false };

    [Theory]
    [InlineData("event", "@event")]
    [InlineData("class", "@class")]
    [InlineData("lock", "@lock")]
    [InlineData("default", "@default")]
    [InlineData("object", "@object")]
    [InlineData("string", "@string")]
    [InlineData("base", "@base")]
    [InlineData("namespace", "@namespace")]
    public void Reserved_keyword_class_names_are_escaped(string table, string expected)
        => Assert.Equal(expected, Class(table, Preserve()));

    [Theory]
    [InlineData("event")]
    [InlineData("class")]
    [InlineData("int")]
    public void Pascal_casing_already_avoids_the_keyword_and_is_left_alone(string table)
    {
        string name = Class(table, new NamingOptions { Singularize = false });
        Assert.DoesNotContain("@", name);
        Assert.Equal(char.ToUpperInvariant(table[0]) + table[1..], name);
    }

    /// <summary>
    /// Contextual keywords are legal identifiers; escaping them would be noise in every generated
    /// file with a column named `value` — which is most audit and settings tables.
    /// </summary>
    [Theory]
    [InlineData("value")]
    [InlineData("record")]
    [InlineData("nameof")]
    [InlineData("var")]
    [InlineData("async")]
    [InlineData("await")]
    [InlineData("dynamic")]
    [InlineData("from")]
    [InlineData("where")]
    [InlineData("field")]
    public void Contextual_keywords_are_not_escaped(string name)
        => Assert.Equal(name, Class(name, Preserve()));

    [Theory]
    [InlineData("event", "@event")]
    [InlineData("params", "@params")]
    [InlineData("value", "value")]
    public void Reserved_keyword_property_names_are_escaped(string column, string expected)
        => Assert.Equal(expected,
            Engine(Preserve()).PropertyNameFor(T("orders"), C(column), "Order"));

    [Fact]
    public void Keyword_escaping_survives_singularization_under_preserve()
    {
        // "events" -> "event" -> reserved.
        var n = new NamingOptions { Style = "preserve", Singularize = true };
        Assert.Equal("@event", Class("events", n));
    }

    [Fact]
    public void Prefix_and_suffix_can_rescue_a_keyword_from_escaping()
    {
        var n = new NamingOptions { Style = "preserve", Singularize = false, ClassSuffix = "Entity" };
        Assert.Equal("eventEntity", Class("event", n));
    }

    [Fact]
    public void An_override_naming_a_keyword_is_escaped_too()
    {
        var o = new OverrideOptions();
        o.Tables["orders"] = new TableOverride { ClassName = "lock" };
        Assert.Equal("@lock", Class("orders", null, o));
    }

    [Fact]
    public void An_override_that_already_escapes_is_not_double_escaped()
    {
        var o = new OverrideOptions();
        o.Tables["orders"] = new TableOverride { ClassName = "@lock" };
        Assert.Equal("@lock", Class("orders", null, o));
    }

    [Fact]
    public void Leading_digit_is_prefixed_to_stay_a_valid_identifier()
        => Assert.Equal("_2fa", NamingEngine.ToPascalCase("2fa"));

    [Theory]
    [InlineData("_")]
    [InlineData("__")]
    [InlineData("-")]
    [InlineData(" ")]
    [InlineData("_-_")]
    public void All_separator_input_falls_back_to_a_valid_identifier(string raw)
    {
        string result = NamingEngine.ToPascalCase(raw);
        Assert.NotEmpty(result);
        Assert.False(char.IsDigit(result[0]));
    }

    [Fact]
    public void Empty_input_stays_empty()
        => Assert.Equal("", NamingEngine.ToPascalCase(""));

    // ---- names that cannot be identifiers at all --------------------------------------
    //
    // --naming-style preserve hands the raw schema name through untouched, and a quoted SQL
    // identifier can be anything at all — "@", "my col", "1st". Each of those used to reach an
    // emitter and produce source that does not compile (and "@" crashed the tool outright while
    // deriving "_" + name[0]). They are refused by name here, where the table and column are
    // still in hand, so the caller is told which column to override.

    [Theory]
    [InlineData("@")]
    [InlineData("my col")]
    [InlineData("1st")]
    [InlineData("total$")]
    public void A_column_name_that_cannot_be_an_identifier_is_refused(string column)
    {
        var ex = Assert.Throws<ScaffoldException>(
            () => Engine(Preserve()).PropertyNameFor(T("orders"), C(column), "orders"));

        Assert.Contains(column, ex.Message);
        Assert.Contains("orders", ex.Message);
        Assert.Contains("overrides.tables", ex.Message);
    }

    [Theory]
    [InlineData("@")]
    [InlineData("my table")]
    [InlineData("2fa")]
    public void A_table_name_that_cannot_be_a_class_name_is_refused(string table)
    {
        var ex = Assert.Throws<ScaffoldException>(() => Class(table, Preserve()));

        Assert.Contains(table, ex.Message);
        Assert.Contains("className", ex.Message);
    }

    [Fact]
    public void An_override_that_is_not_an_identifier_is_refused_too()
    {
        var o = new OverrideOptions();
        o.Tables["orders"] = new TableOverride { ClassName = "Sales Order" };

        Assert.Throws<ScaffoldException>(() => Class("orders", null, o));
    }

    [Theory]
    [InlineData("_")]
    [InlineData("_2fa")]
    [InlineData("order_line")]
    [InlineData("@event")]
    [InlineData("Order2")]
    public void Legal_identifiers_are_left_alone(string name)
        => Assert.Equal(name, Class(name, Preserve()));

    // ---- namespaces are identifiers too ------------------------------------------------

    [Theory]
    [InlineData("Zoo.Data", "Zoo.Data")]
    [InlineData("Zoo.lock.Data", "Zoo.@lock.Data")]
    [InlineData("lock", "@lock")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void Namespace_segments_are_escaped_individually(string? input, string? expected)
        => Assert.Equal(expected, NamingEngine.EscapeNamespace(input));

    [Theory]
    [InlineData("Zoo..Data")]
    [InlineData("Zoo.my data")]
    [InlineData("9zoo.Data")]
    public void A_namespace_segment_that_is_not_an_identifier_is_refused(string ns)
        => Assert.Contains("--namespace",
            Assert.Throws<ScaffoldException>(() => NamingEngine.EscapeNamespace(ns)).Message);
}
