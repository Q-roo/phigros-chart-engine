namespace PCE.Chartbuild;

public class FunctionDeclarationStatementNode(string name, FunctionParameter[] arguments, bool isLastParams, BlockStatementNode body, BaseType returnType) : StatementNode
{
    public readonly string name = name;
    public readonly bool isLastParams = isLastParams;
    public readonly FunctionParameter[] arguments = arguments;
    public readonly BlockStatementNode body = body;

    // infer it
    public BaseType returnType = returnType;
    public bool pure;
}