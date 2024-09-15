namespace PCE.Chartbuild;

public class ComputedMemberAccessExpressionNode(ExpressionNode member, ExpressionNode property) : MemberExpressionNode(member)
{
    public readonly ExpressionNode property = property;
}