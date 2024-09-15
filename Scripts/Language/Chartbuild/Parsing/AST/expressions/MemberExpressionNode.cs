namespace PCE.Chartbuild;

public class MemberExpressionNode(ExpressionNode member) : ExpressionNode
{
    public readonly ExpressionNode member = member;
}