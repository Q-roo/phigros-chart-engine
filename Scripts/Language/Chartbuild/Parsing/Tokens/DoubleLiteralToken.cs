namespace PCE.Chartbuild;

public sealed class DoubleLiteralToken(int lineNumber, int columnNumber, double value) : BaseToken(lineNumber, columnNumber)
{
    public override TokenType Type => TokenType.FloatLiteral;

    public readonly double value = value;
    public override string ToString()
    {
        return $"{base.ToString()}({value})";
    }
}