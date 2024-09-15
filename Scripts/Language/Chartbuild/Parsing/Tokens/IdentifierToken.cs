namespace PCE.Chartbuild;

public sealed class IdentifierToken(int lineNumber, int columnNumber, string name) : BaseToken(lineNumber, columnNumber)
{
    public override TokenType Type => TokenType.Identifier;
    public readonly string name = name;

    public override string ToString()
    {
        return $"{base.ToString()}({name})";
    }
}