using System;

namespace PCE.Chartbuild;

public class BaseException(BaseToken target, string message) : Exception($"{message} (at {target.lineNumber}, {target.columnNumber})")
{
    public readonly BaseToken target = target;

    public BaseException(BaseToken target)
    : this(target, string.Empty) {}
}