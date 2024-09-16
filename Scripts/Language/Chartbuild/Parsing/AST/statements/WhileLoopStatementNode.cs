namespace PCE.Chartbuild;

public class WhileLoopStatementNode(ExpressionNode condition, StatementNode body) : LoopStatementNode(body)
{
    public ExpressionNode condition = condition;
}