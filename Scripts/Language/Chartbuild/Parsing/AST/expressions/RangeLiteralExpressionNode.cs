namespace PCE.Chartbuild;

public class RangeLiteralExpressionNode(ExpressionNode start, ExpressionNode end, bool inclusiveEnd) : ExpressionNode
{
    public readonly ExpressionNode start = start;
    public readonly ExpressionNode end = end;
    public readonly bool inclusiveEnd = inclusiveEnd;
}