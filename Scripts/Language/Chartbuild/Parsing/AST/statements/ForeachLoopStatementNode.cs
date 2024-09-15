namespace PCE.Chartbuild;

public class ForeachLoopStatementNode(VariableDeclarationStatementNode value, ExpressionNode iterable, StatementNode body) : LoopStatementNode(body)
{
    public readonly VariableDeclarationStatementNode value = value;
    public readonly ExpressionNode iterable = iterable;
}