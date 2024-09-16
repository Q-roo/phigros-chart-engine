namespace PCE.Chartbuild;

// ( nud is already used and I did not want to deal with it
// on the other hand, | and || are still free
public class ClosureExpressionNode(FunctionParameter[] parameters, BaseType returnType, StatementNode body) : ExpressionNode
{
    // type is not always required as it can be inferred
    public readonly FunctionParameter[] parameters = parameters;
    public readonly StatementNode body = body;

    public readonly BaseType returnType = returnType;
}