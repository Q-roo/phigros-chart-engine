using System;
using System.Collections.Generic;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild.Bindings;

using Object = Runtime.Object;

public class NativeObject(object value, Func<object, Object> getter, Action<object, Object> setter) : Object {
    private readonly object value = value;
    private readonly Func<object, Object> getter = getter;
    private readonly Action<object, Object> setter = setter;
    public override Object this[object key] {
        get {
            Object result = getter(key);
            result.parentKey = key;
            result.parentObject = this;

            return result;
        }
        set => setter(key, value);
    }

    public override object Value => value;

    public NativeObject(object value)
    : this(value, key => throw KeyNotFound(key), (key, _) => throw KeyNotFound(key)) { }

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new NativeObject(value, getter, setter); // TODO: support deep copy
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        return @operator switch {
            OperatorType.Equal => new Bool(value.Equals(rhs.Value)),
            OperatorType.NotEqual => new Bool(!value.Equals(rhs.Value)),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        throw NotSupportedOperator(@operator);
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() {
        return value.ToString();
    }
}