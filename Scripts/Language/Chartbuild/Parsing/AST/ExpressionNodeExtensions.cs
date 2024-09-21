namespace PCE.Chartbuild;

public static class ExpressionNodeExtensions {
    public static bool IsNullOrEmpty(this ExpressionNode expression) => expression is null || expression is EmptyExpressionNode;
}