namespace PCE.Chartbuild;

public class AssignmentExpressionNode(ExpressionNode asignee, BaseToken @operator, ExpressionNode value) : ExpressionNode
{
    public readonly ExpressionNode asignee = asignee;
    public readonly BaseToken @operator = @operator;
    public readonly ExpressionNode value = value;
}