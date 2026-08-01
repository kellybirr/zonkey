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

    public override string ToString() => _sb.ToString();
}
