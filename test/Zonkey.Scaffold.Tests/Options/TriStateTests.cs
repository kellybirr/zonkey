using Xunit;
using Zonkey.Scaffold.Options;

public class TriStateTests
{
    [Theory]
    [InlineData("auto", TriState.Auto)]
    [InlineData("AUTO", TriState.Auto)]
    [InlineData("true", TriState.True)]
    [InlineData("False", TriState.False)]
    [InlineData(null, TriState.Auto)]
    [InlineData("", TriState.Auto)]
    public void Parse_maps_text_to_state(string? input, TriState expected)
        => Assert.Equal(expected, TriStateExtensions.Parse(input));

    [Fact]
    public void Parse_rejects_garbage()
        => Assert.Throws<FormatException>(() => TriStateExtensions.Parse("maybe"));

    [Theory]
    [InlineData(TriState.Auto, true, true)]
    [InlineData(TriState.Auto, false, false)]
    [InlineData(TriState.True, false, true)]
    [InlineData(TriState.False, true, false)]
    public void Resolve_honours_explicit_over_detected(TriState state, bool detected, bool expected)
        => Assert.Equal(expected, state.Resolve(detected));
}
