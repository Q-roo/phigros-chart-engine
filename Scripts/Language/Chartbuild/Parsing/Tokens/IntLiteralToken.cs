namespace PCE.Chartbuild;

public sealed class IntLiteralToken(int lineNumber, int columnNumber, int value) : BaseToken(lineNumber, columnNumber)
{
    public override TokenType Type => TokenType.IntLiteral;

    public readonly int value = value;
    public override string ToString()
    {
        return $"{base.ToString()}({value})";
    }
}