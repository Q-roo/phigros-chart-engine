namespace PCE.Chartbuild;

public class ComputedMemberAccessExpressionNode(ExpressionNode member, ExpressionNode property) : MemberExpressionNode(member)
{
    public ExpressionNode property = property;
}