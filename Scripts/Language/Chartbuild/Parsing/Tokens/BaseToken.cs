namespace PCE.Chartbuild;

public abstract class BaseToken(int lineNumber, int columnNumber)
{
    public abstract TokenType Type { get; }
    public readonly int lineNumber = lineNumber;
    public readonly int columnNumber = columnNumber;

    public override string ToString()
    {
        return $"Token[{Type.ToSourceString()} line {lineNumber} column {columnNumber}]";
    }
}