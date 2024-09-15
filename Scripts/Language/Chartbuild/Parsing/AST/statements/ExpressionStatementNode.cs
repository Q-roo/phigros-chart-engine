namespace PCE.Chartbuild;

public class ExpressionStatementNode(ExpressionNode expression) : StatementNode
{
    public readonly ExpressionNode expression = expression;
}