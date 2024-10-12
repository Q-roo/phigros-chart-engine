using System;

namespace PCE.Chartbuild.Runtime;

public class Closure : Object {
    private readonly Scope scope;
    private readonly ClosureExpressionNode closure;
    private readonly ASTWalker walker;

    public Closure(Scope scope, ClosureExpressionNode closure, ASTWalker walker)
    : base(null) {
        this.scope = scope;
        this.closure = closure;
        this.walker = walker;
        NativeValue = new Func<Object[], Object>(Call);
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return this; // NOTE: this one shouldn't be copied
    }

    public override Object Call(params Object[] args) {
        return walker.CallUserDefinedClosure(new(scope), closure, args);
    }

    public override Object BinaryOperation(OperatorType @operator, Object rhs) {
        return @operator switch {
            OperatorType.Equal => Equals(rhs),
            OperatorType.NotEqual => !Equals(rhs),
            _ => base.BinaryOperation(@operator, rhs)
        };
    }

    public override string ToString() => "Callable";
}