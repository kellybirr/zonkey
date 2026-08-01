using Xunit;
using Zonkey.Scaffold.Naming;

public class InflectorTests
{
    private static Inflector New(Dictionary<string, string>? irregulars = null)
        => new(irregulars ?? new Dictionary<string, string>());

    [Theory]
    [InlineData("animals", "animal")]
    [InlineData("species", "species")]        // uncountable — the old naive rule gave "specy"
    [InlineData("status", "status")]
    [InlineData("addresses", "address")]
    [InlineData("boxes", "box")]
    [InlineData("people", "person")]
    [InlineData("analyses", "analysis")]
    [InlineData("news", "news")]
    [InlineData("cities", "city")]
    [InlineData("equipment", "equipment")]
    public void Singularize_uses_real_inflection(string input, string expected)
        => Assert.Equal(expected, New().Singularize(input));

    [Fact]
    public void Only_the_final_token_is_inflected()
        => Assert.Equal("feeding_schedule", New().Singularize("feeding_schedules"));

    [Fact]
    public void Multiword_head_is_untouched_even_when_plural()
        => Assert.Equal("orders_line", New().Singularize("orders_lines"));

    [Theory]
    [InlineData("taxes", "tax")]
    [InlineData("data", "data")]
    [InlineData("media", "media")]
    public void Irregulars_override_the_library(string input, string expected)
    {
        var inflector = New(new Dictionary<string, string>
        {
            ["taxes"] = "tax", ["data"] = "data", ["media"] = "media"
        });
        Assert.Equal(expected, inflector.Singularize(input));
    }

    [Fact]
    public void Irregular_lookup_is_case_insensitive()
        => Assert.Equal("tax", New(new() { ["taxes"] = "tax" }).Singularize("Taxes"));

    [Fact]
    public void Pluralize_round_trips_the_common_case()
        => Assert.Equal("orders", New().Pluralize("order"));

    [Theory]
    [InlineData("animals", "animal", false)]      // plain trailing -s
    [InlineData("boxes", "box", false)]           // plain trailing -es
    [InlineData("cities", "city", false)]         // plain -ies -> -y
    [InlineData("species", "species", false)]     // unchanged
    [InlineData("people", "person", true)]        // genuinely irregular
    [InlineData("analyses", "analysis", true)]
    [InlineData("mice", "mouse", true)]
    public void IsUncertain_flags_only_non_obvious_changes(string input, string result, bool expected)
        => Assert.Equal(expected, New().IsUncertain(input, result));
}
