namespace PCE.Chartbuild;

public class VariableDeclarationStatementNode(string name, ExpressionNode valueExpression) : StatementNode
{
    public readonly string name = name;

    public ExpressionNode valueExpression = valueExpression;
}