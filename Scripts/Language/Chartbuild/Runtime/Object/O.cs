using System.Collections;
using System.Collections.Generic;
using Godot;

namespace PCE.Chartbuild.Runtime;

public abstract class Property(Object @this, object key) : Object(null) {
    protected readonly Object @this = @this;
    protected readonly object key = key;
    protected abstract Object Getter();
    protected abstract void Setter(Object value);

    public Object Get() => Getter();
    public void Set(Object value) {
        Setter(value);
    }

    public virtual Property Copy() => this;
    public override Object Copy(bool shallow = true, params object[] keys) => Copy();
}

public class ValueProperty(Object @this, object key, Object value) : Property(@this, key) {
    private Object value = value;

    protected override Object Getter() => value;

    protected override void Setter(Object value) => this.value = value;

    public override Property Copy() => new ValueProperty(@this, key, value.Copy());
}

public class ReadOnlyValueProperty(Object @this, object key, Object value) : ValueProperty(@this, key, value) {
    protected override void Setter(Object _) => throw new System.MemberAccessException($"{@this.GetType()}[{key}] is read-only");
}

public delegate Object Getter<T>(Object @this, T key);
public delegate void Setter<T>(Object @this, T key, Object value);

public class SetGetProperty(Object @this, object key, Getter<object> getter, Setter<object> setter) : Property(@this, key) {
    private readonly Getter<object> getter = getter;
    private readonly Setter<object> setter = setter;

    protected override Object Getter() => getter(@this, key);

    protected override void Setter(Object value) => setter(@this, key, value);
}

public class ReadOnlyProperty(Object @this, object key, Getter<object> getter) : SetGetProperty(@this, key, getter, (@this, key, _) => throw new System.MemberAccessException($"{@this.GetType()}[{key}] is read-only"));

public abstract class Object(object nativeValue) : IEnumerable<Object> {
    public delegate void OnChange(object oldValue, object newValue);
    public event OnChange OnValueChanged;

    private object _nativeValue = nativeValue;
    public object NativeValue {
        get => _nativeValue;
        protected set {
            object oldValue = _nativeValue;
            _nativeValue = value;
            OnValueChanged?.Invoke(oldValue, _nativeValue);
        }
    }
    
    public void SetNativeValue(Object obj) => NativeValue = obj.NativeValue;

    public Object GetValue() => this is Property property ? property.Get().GetValue() : this;

    public virtual Property GetProperty(object key) => throw KeyNotFound(key);

    public virtual Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Not => !ToBool(),
        _ => throw new System.NotSupportedException($"{GetType()} does not support unary operator \"{@operator.ToSourceString()}\"")
    };
    public virtual Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.Equal => Equals(rhs),
        OperatorType.NotEqual => !Equals(rhs),
        _ => throw new System.NotSupportedException($"{GetType()} does not support binary operator \"{@operator.ToSourceString()}\"")
    };

    public virtual List<Object> ToList() => throw new System.InvalidCastException();
    public virtual bool ToBool() => throw new System.InvalidCastException();
    public virtual int ToI32() => throw new System.InvalidCastException();
    public virtual float ToF32() => throw new System.InvalidCastException();
    public virtual Vector2 ToVec2() => throw new System.InvalidCastException();

    public static implicit operator List<Object>(Object obj) => obj.ToList();
    public static implicit operator Object(List<Object> list) => new Array(list);

    public static implicit operator bool(Object obj) => obj.ToBool();
    public static implicit operator Object(bool b) => new Bool(b);

    public static implicit operator int(Object obj) => obj.ToI32();
    public static implicit operator Object(int i) => new I32(i);

    public static implicit operator float(Object obj) => obj.ToF32();
    public static implicit operator Object(float f) => new F32(f);

    public static implicit operator string(Object obj) => obj.ToString();
    public static implicit operator Object(string s) => new Str(s);

    public static implicit operator Vector2(Object obj) => obj.ToVec2();
    public static implicit operator Object(Vector2 v) => new Vec2(v);

    public virtual Object Call(params Object[] args) => throw new System.NotSupportedException($"{GetType()} is not callable");
    public virtual IEnumerator<Object> GetEnumerator() => throw new System.NotSupportedException($"{GetType()} is not enumerable");
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public abstract Object Copy(bool shallow = true, params object[] keys);

    public KeyNotFoundException KeyNotFound(object key) => new($"unknown property on {GetType()}: {key}");

    public override string ToString() => NativeValue.ToString();
    public sealed override int GetHashCode() => NativeValue.GetHashCode();

    // even though it can be simplified, it's better to be explicit here
    public bool Equals(Object rhs) => object.Equals(NativeValue, rhs.NativeValue);
    public sealed override bool Equals(object obj) => obj is Object o ? Equals(o) : object.Equals(NativeValue, obj);
}

public abstract class Object<T>(T value) : Object(value) {
    private T _value = value;
    public T Value {
        get => _value;
        set {
            _value = value;
            NativeValue = value;
        }
    }

    public Object() : this(default) { }
}