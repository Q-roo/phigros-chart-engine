namespace PCE.Chartbuild;

public static class StatementNodeExtensions {
    public static bool IsNullOrEmpty(this StatementNode statement) => statement is null || statement is EmptyStatementNode;
}