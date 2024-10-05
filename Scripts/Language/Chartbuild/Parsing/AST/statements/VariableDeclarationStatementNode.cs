namespace PCE.Chartbuild;

public class VariableDeclarationStatementNode(string name, ExpressionNode valueExpression, bool @readonly) : StatementNode
{
    public readonly string name = name;
    public readonly bool @readonly = @readonly;

    public ExpressionNode valueExpression = valueExpression;
}