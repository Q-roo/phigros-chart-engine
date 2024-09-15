namespace PCE.Chartbuild;

public sealed class Token(int lineNumber, int columnNumber, TokenType type) : BaseToken(lineNumber, columnNumber)
{
    public override TokenType Type => type;
}