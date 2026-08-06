namespace Zonkey.Scaffold.Diagnostics;

/// <summary>
/// A user-facing failure. The message is printed as-is, so it must always name the remedy —
/// an error an agent cannot act on is only marginally better than a wrong answer.
/// </summary>
public sealed class ScaffoldException : Exception
{
    public ScaffoldException(string message) : base(message) { }
    public ScaffoldException(string message, Exception inner) : base(message, inner) { }
}
