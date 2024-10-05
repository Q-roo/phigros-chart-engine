using System;
using System.Collections.Generic;

namespace PCE.Chartbuild.Bindings;

using Object = Runtime.Object;

public class NativeObject(object value, Func<object, Object> getter, Action<object, Object> setter) : Object {
    private readonly object value = value;
    private readonly Func<object, Object> getter = getter;
    private readonly Action<object, Object> setter = setter;
    public override Object this[object key] { get => getter(key); set => setter(key, value); }

    public override object Value => value;

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new NativeObject(value, getter, setter); // TODO: support deep copy
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

    public override Object SetValue(Object value) {
        throw ReadOnlyValue();
    }

    public override string ToString() {
        return value.ToString();
    }
}