namespace PCE.Chartbuild;

public class MemberAccessExpressionNode(ExpressionNode member, string property) : MemberExpressionNode(member)
{
    public readonly string property = property;
}