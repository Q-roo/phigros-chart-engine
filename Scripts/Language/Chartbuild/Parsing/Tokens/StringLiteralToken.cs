namespace PCE.Chartbuild;

public sealed class StringLiteralToken(int lineNumber, int columnNumber, string value) : BaseToken(lineNumber, columnNumber)
{
    public override TokenType Type => TokenType.StringLiteral;

    public readonly string value = value;
    public override string ToString()
    {
        return $"{base.ToString()}({value})";
    }
}