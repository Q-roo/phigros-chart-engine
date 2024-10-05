using System;
using System.Collections.Generic;
using PCE.Chartbuild.Runtime;

using Object = PCE.Chartbuild.Runtime.Object;

namespace PCE.Chartbuild.Bindings;

public class NativeFunction : Object {
    private readonly Func<Object[], Object> function;
    public override object Value => function;

    public override Object this[object key] { get => throw KeyNotFound(key); set => throw KeyNotFound(key); }

    public NativeFunction(Func<Object[], Object> function) {
        this.function = function;
    }

    public NativeFunction(Action<Object[]> function) {
        this.function = args => {
            function(args);
            return new Unset();
        };
    }

    // public NativeFunction(Function function)
    // : this(new Func<Object[], Object>(function)) { }

    // public NativeFunction(FunctionImplicitNullReturn function)
    // : this(new Action<Object[]>(function)) { }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new NativeFunction(shallow ? function : new Func<Object[], Object>(function));
    }

    public override Object Call(params Object[] args) {
        return function(args);
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        throw NotSupportedOperator(@operator);
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        throw NotSupportedOperator(@operator);
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => function.ToString();
}