using Xunit;
using Zonkey.Scaffold.Selection;

public class GlobPatternTests
{
    [Theory]
    [InlineData("__*", "__migrations", true)]
    [InlineData("__*", "migrations", false)]
    [InlineData("aspnet_*", "AspNet_Users", true)]     // case-insensitive
    [InlineData("*", "anything", true)]
    [InlineData("orders", "orders", true)]
    [InlineData("orders", "orders_archive", false)]
    [InlineData("order?", "orders", true)]
    [InlineData("order?", "order", false)]
    [InlineData("*_audit", "customer_audit", true)]
    [InlineData("*.tenant_id", "orders.tenant_id", true)]
    [InlineData("*.tenant_id", "orders.customer_id", false)]
    public void IsMatch_handles_wildcards(string pattern, string value, bool expected)
        => Assert.Equal(expected, GlobPattern.IsMatch(pattern, value));

    [Fact]
    public void Regex_metacharacters_in_pattern_are_literal()
        => Assert.True(GlobPattern.IsMatch("a+b", "a+b"));

    [Fact]
    public void Dot_is_literal_not_any_char()
        => Assert.False(GlobPattern.IsMatch("a.b", "axb"));

    [Fact]
    public void Trailing_newline_does_not_match_literal_pattern()
        => Assert.False(GlobPattern.IsMatch("orders", "orders\n"));

    [Fact]
    public void Trailing_newline_does_not_match_wildcard_pattern()
        => Assert.False(GlobPattern.IsMatch("orders*", "orders\n"));
}
