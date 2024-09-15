namespace PCE.Chartbuild;

public class TernaryExpressionNode(ExpressionNode condition, ExpressionNode @true, ExpressionNode @false) : ExpressionNode
{
    public ExpressionNode condition = condition;
    public ExpressionNode @true = @true;
    public ExpressionNode @false = @false;
}