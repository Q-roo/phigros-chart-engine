namespace PCE.Chartbuild;

public class ValueExpressionNode<T>(T value) : ExpressionNode
{
    public readonly T value = value;
}