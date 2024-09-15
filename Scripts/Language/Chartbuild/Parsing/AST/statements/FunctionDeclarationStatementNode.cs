namespace PCE.Chartbuild;

public class FunctionDeclarationStatementNode(string name, FunctionParameter[] arguments, BlockStatementNode body, BaseType returnType) : StatementNode
{
    public readonly string name = name;
    public readonly FunctionParameter[] arguments = arguments;
    public readonly BlockStatementNode body = body;

    // infer it
    public BaseType returnType { get; private set; } = returnType;
    public bool pure { get; private set; }
}