namespace PCE.Chartbuild;

public class CallExpressionNode(ExpressionNode method, ExpressionNode[] arguments) : ExpressionNode
{
    public readonly ExpressionNode method = method;
    public readonly ExpressionNode[] arguments = arguments;
}