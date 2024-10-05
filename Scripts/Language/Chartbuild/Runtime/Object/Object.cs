using System;
using System.Collections;
using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

// NOTE: should this be an interface instead?
public abstract class Object : IEnumerable<Object> {
    public abstract Object this[object key] { get; set; }
    public abstract object Value { get; }

    public Object parentObject;
    public object parentKey;

    public Object SetValue(Object value) {
        parentObject[parentKey] = value;
        value.parentObject = parentObject;
        value.parentKey = parentKey;

        return value;
    }

    // make a copy of the object
    // either a shallow or a deep one
    // the values passed into keys will use the opposite copying strategy
    // e.g. Array.Copy(true, 0) would make a shallow copy of the whole list but the value at 0 would be deep copied
    public abstract Object Copy(bool shallow=true, params object[] keys);

    public abstract Object Call(params Object[] args);
    public abstract Object ExecuteBinary(OperatorType @operator, Object rhs);
    public abstract Object ExecuteUnary(OperatorType @operator, bool prefix);

    public abstract IEnumerator<Object> GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected static KeyNotFoundException KeyNotFound(object key) => new($"key \"{key}\" is not associated with any properties");
    protected static InvalidOperationException ReadOnlyProperty(object key) => new($"{key} is a read-only property");
    protected InvalidOperationException ReadOnlyValue() => new($"{GetType()} is a read-only value");
    protected static NotSupportedException NotSupportedOperator(OperatorType @operator) => new($"{@operator.ToSourceString()} is not supported");
    protected NotSupportedException NotCallable() => new($"{GetType()} is not callable");
    protected NotSupportedException NotIterable() => new($"{GetType()} is not iterable");
    protected InvalidCastException NotCastable(Type type) => new($"{type} is not castable to {GetType()}");
    protected InvalidCastException NotCastable(Object @object) => NotCastable(@object.GetType());

    public virtual Array ToArray() => throw NotCastable(typeof(Array));
    public virtual Bool ToBool()=> throw NotCastable(typeof(Bool));
    public virtual Closure ToClosure()=> throw NotCastable(typeof(Closure));
    public virtual F32 ToF32()=> throw NotCastable(typeof(F32));
    public virtual I32 ToI32()=> throw NotCastable(typeof(I32));
    public virtual Str ToStr() => new(ToString());
    public virtual Vec2 ToVec2() => new(new(ToF32().value, ToF32().value));

    public abstract override string ToString();
}