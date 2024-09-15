namespace PCE.Chartbuild;

public class BinaryExpressionNode(ExpressionNode left, BaseToken @operator, ExpressionNode right) : ExpressionNode
{
    public readonly ExpressionNode left = left;
    public readonly ExpressionNode right = right;
    public readonly BaseToken @operator = @operator;
}