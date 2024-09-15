namespace PCE.Chartbuild;

public class WhileLoopStatementNode(ExpressionNode condition, StatementNode body) : LoopStatementNode(body)
{
    public readonly ExpressionNode condition = condition;
}