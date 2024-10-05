namespace PCE.Chartbuild.Runtime;

public class ASTClosureValue : ObjectValue {
    private readonly Scope scope;
    private readonly ClosureExpressionNode closure;
    private readonly ASTWalker walker;

    public ASTClosureValue(Scope scope, ClosureExpressionNode closure, ASTWalker walker)
    : base(scope) {
        this.scope = scope;
        this.closure = closure;
        this.walker = walker;
        Type = ValueType.Callable;
    }

    public override CBObject Call(params CBObject[] args) {
        // // scopes need to be reconstructed
        // return walker.CallUserDefinedClosure(new(scope), closure, args);
        throw new System.Exception("");
    }
}