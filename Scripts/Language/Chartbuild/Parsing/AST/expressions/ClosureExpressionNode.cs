namespace PCE.Chartbuild;

// ( nud is already used and I did not want to deal with it
// on the other hand, | and || are still free
public class ClosureExpressionNode(FunctionParameter[] arguments, StatementNode body, bool isLastParams) : ExpressionNode
{
    // type is not always required as it can be inferred
    public readonly FunctionParameter[] arguments = arguments;
    public readonly bool isLastParams = isLastParams;
    public readonly StatementNode body = body;
}