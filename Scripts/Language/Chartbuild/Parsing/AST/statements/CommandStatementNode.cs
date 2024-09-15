namespace PCE.Chartbuild;

public class CommandStatementNode(string name, ExpressionNode expression) : StatementNode
{
    public readonly string name = name;
    public readonly ExpressionNode expression = expression;
}