namespace PCE.Chartbuild;

public class ForLoopStatementNode(VariableDeclarationStatementNode init, ExpressionNode condition, ExpressionNode update, StatementNode body) : LoopStatementNode(body)
{
    public readonly VariableDeclarationStatementNode init = init;
    public ExpressionNode condition = condition;
    public ExpressionNode update = update;
}