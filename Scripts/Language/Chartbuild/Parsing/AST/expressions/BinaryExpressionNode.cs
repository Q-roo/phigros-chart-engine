namespace PCE.Chartbuild;

public class BinaryExpressionNode(ExpressionNode left, BaseToken @operator, ExpressionNode right) : ExpressionNode
{
    public ExpressionNode left = left;
    public ExpressionNode right = right;
    public readonly BaseToken @operator = @operator;
}