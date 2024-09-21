namespace PCE.Chartbuild;

public class ReturnStatementNode(ExpressionNode value) : StatementNode
{
    // null if it's just return;
    public ExpressionNode value = value;
}