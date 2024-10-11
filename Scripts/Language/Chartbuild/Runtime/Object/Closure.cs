using System;

namespace PCE.Chartbuild.Runtime;

public class Closure : O {
    private readonly Scope scope;
    private readonly ClosureExpressionNode closure;
    private readonly ASTWalker walker;

    public Closure(Scope scope, ClosureExpressionNode closure, ASTWalker walker)
    : base(null) {
        this.scope = scope;
        this.closure = closure;
        this.walker = walker;
        nativeValue = new Func<O[], O>(Call);
    }

    public override O Copy(bool shallow = true, params object[] keys) {
        return this; // NOTE: this one shouldn't be copied
    }

    public override O Call(params O[] args) {
        return walker.CallUserDefinedClosure(new(scope), closure, args);
    }

    public override O BinaryOperation(OperatorType @operator, O rhs) {
        return @operator switch {
            OperatorType.Equal => Equals(rhs),
            OperatorType.NotEqual => !Equals(rhs),
            _ => base.BinaryOperation(@operator, rhs)
        };
    }

    public override string ToString() => "Callable";
}