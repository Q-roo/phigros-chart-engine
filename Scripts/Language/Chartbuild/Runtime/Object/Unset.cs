using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Unset : Object {
    public override Object this[object key] { get => throw KeyNotFound(key); set => throw KeyNotFound(key); }

    public override object Value => null;

    public override Object Copy(bool shallow = true, params object[] keys) {
        return this; // NOTE: this class doesn't really need copies
    }

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        if (@operator == OperatorType.Equal)
        return new Bool(rhs.Value is null);
        if (@operator == OperatorType.NotEqual)
        return new Bool(rhs.Value is not null);

        return rhs switch {
            Array => new Array().ExecuteBinary(@operator, rhs),
            Bool => new Bool(false).ExecuteBinary(@operator, rhs),
            F32 => new F32(0).ExecuteBinary(@operator, rhs),
            I32 => new I32(0).ExecuteBinary(@operator, rhs),
            Str => new Str(string.Empty).ExecuteBinary(@operator, rhs),
            _ => throw NotCastable(rhs)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return @operator switch {
            // unset is falsy so this should be !false AKA true
            OperatorType.Not => new Bool(true),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => "unset";
}