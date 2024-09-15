using System;

namespace PCE.Chartbuild;

public class Error : Exception
{
    public Error() {}

    public Error(ErrorType type, string message, int line, int column)
    {
        this.type = type;
        this.message = message;
        this.line = line;
        this.column = column;
    }

    public Error(BaseToken token, ErrorType type, string message)
    : this(type, message, token.lineNumber, token.columnNumber) {}

    public readonly ErrorType type;
    public readonly int line;
    public readonly int column;
    public readonly string message;
}