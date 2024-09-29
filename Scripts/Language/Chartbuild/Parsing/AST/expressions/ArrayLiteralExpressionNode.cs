namespace PCE.Chartbuild;

public class ArrayLiteralExpressionNode(params ExpressionNode[] content) : ExpressionNode
{
    public readonly ExpressionNode[] content = content;
}