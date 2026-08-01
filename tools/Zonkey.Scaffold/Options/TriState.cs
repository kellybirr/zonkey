namespace Zonkey.Scaffold.Options;

public enum TriState { Auto = 0, True = 1, False = 2 }

public static class TriStateExtensions
{
    public static TriState Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "auto" => TriState.Auto,
        "true"  => TriState.True,
        "false" => TriState.False,
        _ => throw new FormatException(
            $"Expected 'true', 'false', or 'auto'; got '{value}'.")
    };

    /// <summary>Explicit wins; Auto defers to what was detected from the project.</summary>
    public static bool Resolve(this TriState state, bool detected) => state switch
    {
        TriState.True  => true,
        TriState.False => false,
        _              => detected
    };

    public static bool IsExplicit(this TriState state) => state != TriState.Auto;
}
