namespace PCE.Chartbuild;

public sealed class LiteralToken(int lineNumber, int columnNumber, string value) : BaseToken(lineNumber, columnNumber)
{
    public override TokenType Type => TokenType.Literal;

    public readonly string value = value;
    public override string ToString()
    {
        return $"{base.ToString()}({value})";
    }
}