namespace PCE.Chartbuild;

public class ArrayLiteralExpressionNode(params ExpressionNode[] content) : ExpressionNode
{
    // reduce it after parsing
    public BaseType type {get; private set;}
    public readonly ExpressionNode[] content = content;
}