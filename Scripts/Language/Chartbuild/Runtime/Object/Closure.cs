using System;
using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Closure(Scope scope, ClosureExpressionNode closure, ASTWalker walker) : Object {
    private readonly Scope scope = scope;
    private readonly ClosureExpressionNode closure = closure;
    private readonly ASTWalker walker = walker;

    public override Object this[object key] { get => throw KeyNotFound(key); set => throw KeyNotFound(key); }

    protected override Object RequestSetValue(Object value) {
        throw ReadOnlyValue();
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return this; // NOTE: this one shouldn't be copied
    }

    public override object Value => new Func<Object[], Object>(Call);

    public override Object Call(params Object[] args) {
        return walker.CallUserDefinedClosure(new(scope), closure, args);
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        return @operator switch {
            OperatorType.Equal => new Bool(Equals(rhs.Value)),
            OperatorType.NotEqual => new Bool(!Equals(rhs.Value)),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        throw NotSupportedOperator(@operator);
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => Value.ToString();

    public override Closure ToClosure() {
        return this;
    }
}