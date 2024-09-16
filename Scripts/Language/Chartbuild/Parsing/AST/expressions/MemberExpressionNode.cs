namespace PCE.Chartbuild;

public class MemberExpressionNode(ExpressionNode member) : ExpressionNode
{
    public ExpressionNode member = member;
}