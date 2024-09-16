namespace PCE.Chartbuild;

public class ForeachLoopStatementNode(VariableDeclarationStatementNode value, ExpressionNode iterable, StatementNode body) : LoopStatementNode(body)
{
    public readonly VariableDeclarationStatementNode value = value;
    public ExpressionNode iterable = iterable;
}