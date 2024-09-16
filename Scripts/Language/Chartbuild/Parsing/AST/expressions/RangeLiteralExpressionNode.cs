namespace PCE.Chartbuild;

public class RangeLiteralExpressionNode(ExpressionNode start, ExpressionNode end, bool inclusiveEnd) : ExpressionNode
{
    // TODO: the step value will be 1 by default and can be set with the set_step_size() function
    public ExpressionNode start = start;
    public ExpressionNode end = end;
    public readonly bool inclusiveEnd = inclusiveEnd;
}