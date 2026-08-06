using System.Text;

namespace Zonkey.Scaffold.Emit;

/// <summary>
/// Always writes '\n'. Generated files must be byte-identical regardless of the machine that
/// produced them, so platform line endings are never used.
/// </summary>
public sealed class IndentedWriter(string indent = "    ")
{
    private readonly StringBuilder _sb = new();
    private int _level;

    public void Line(string text)
    {
        for (int i = 0; i < _level; i++) _sb.Append(indent);
        _sb.Append(text).Append('\n');
    }

    public void Blank() => _sb.Append('\n');

    public void Open(string text)
    {
        Line(text);
        Line("{");
        _level++;
    }

    public void Close(string suffix = "")
    {
        _level--;
        Line("}" + suffix);
    }

    /// <summary>Writes a line and indents, with no brace — for VB, which closes with a keyword.</summary>
    public void Push(string text)
    {
        Line(text);
        _level++;
    }

    /// <summary>Outdents, then writes the closing keyword line.</summary>
    public void Pop(string text)
    {
        _level--;
        Line(text);
    }

    public override string ToString() => _sb.ToString();
}
