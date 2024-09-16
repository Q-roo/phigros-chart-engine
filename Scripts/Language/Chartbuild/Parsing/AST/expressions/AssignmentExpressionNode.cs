namespace PCE.Chartbuild;

public class AssignmentExpressionNode(ExpressionNode asignee, BaseToken @operator, ExpressionNode value) : ExpressionNode
{
    public ExpressionNode asignee = asignee;
    public readonly BaseToken @operator = @operator;
    public ExpressionNode value = value;
}