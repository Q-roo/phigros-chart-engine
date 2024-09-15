namespace PCE.Chartbuild;

public class IfStatementNode(ExpressionNode condition, StatementNode @true, StatementNode @false) : StatementNode
{
    public ExpressionNode condition = condition;
    public StatementNode @true = @true;
    public StatementNode @false = @false;
}