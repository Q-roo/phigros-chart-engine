namespace PCE.Chartbuild;

public class ExpressionStatementNode(ExpressionNode expression) : StatementNode
{
    public ExpressionNode expression = expression;
}