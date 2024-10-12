using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

public class KVObject() : Object(null) {
    protected readonly Dictionary<object, Property> properties = [];
    public override Property GetProperty(object key) => properties[key];

    public void AddProperty(object key, Property property) {
        properties[key] = property;
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        throw new System.NotImplementedException();
    }
}

public class Array : Object<List<Object>> {
    private readonly ReadOnlyProperty _length;
    private readonly ReadOnlyValueProperty _pushBack;
    private readonly ReadOnlyValueProperty _popFront;
    private readonly ReadOnlyValueProperty _extend;

    public Array(List<Object> list)
    : base(list) {
        _length = new(this, "length", (_, key) => {
            Debug.Assert(key.Equals("length"));
            return Value.Count;
        });

        _pushBack = new(this, "push_front", new Callable(args => Value.Add(args.Length > 0 ? args[0] : new Unset())));
        _popFront = new(this, "pop_pack", new Callable(_ => {
            Object ret = Value[^1];
            Value.RemoveAt(Value.Count - 1);
            return ret;
        }));
        _extend = new(this, "extend", new Callable(Value.AddRange));
    }

    public Array(IEnumerable<Object> content)
    : this(new(content)) { }

    public Array()
    : this([]) { }

    public override Property GetProperty(object key) => key switch {
        // should always be the idx from the switch
        int idx => new SetGetProperty(this, idx, (_, idx) => Value[(int)idx], (_, idx, value) => Value[(int)idx] = value),
        "length" => _length,
        "push_back" => _pushBack,
        "pop_front" => _popFront,
        "extend" => _extend,
        _ => base.GetProperty(key)
    };

    public override List<Object> ToList() => Value;
    public override bool ToBool() => Value is not null && Value.Count > 0;
    public override string ToString() => $"[{string.Join(", ", Value)}]";

    public override IEnumerator<Object> GetEnumerator() => Value.GetEnumerator();

    public override Object Copy(bool shallow = true, params object[] keys) {
        List<Object> ret = new(Value.Map(it => it.Copy(shallow)));

        // the keys might not be ordered
        foreach (object obj in keys) {
            int idx = (int)obj;
            ret[idx] = Value[idx].Copy(!shallow);
        }

        return ret;
    }
}

public class Bool(bool value) : Object<bool>(value) {
    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.And => Value && rhs,
        OperatorType.Or => Value || rhs,
        _ => new I32(this).BinaryOperation(@operator, rhs),
    };

    public override bool ToBool() => Value;
    public override float ToF32() => Value ? 0f : 1f;
    public override int ToI32() => Value ? 0 : 1;
    public override Vector2 ToVec2() => Value ? Vector2.Zero : Vector2.One;

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}

public class I32(int value) : Object<int>(value) {
    public override Object BinaryOperation(OperatorType @operator, Object rhs) {
        if (rhs is F32)
            return new F32(Value).BinaryOperation(@operator, rhs);

        return @operator switch {
            OperatorType.LessThan => Value < rhs,
            OperatorType.LessThanOrEqual => Value <= rhs,
            OperatorType.GreaterThan => Value > rhs,
            OperatorType.GreaterThanOrEqual => Value >= rhs,
            OperatorType.BitwiseAnd => Value & rhs,
            OperatorType.BitwiseOr => Value | rhs,
            OperatorType.BitwiseXor => Value ^ rhs,
            OperatorType.ShiftLeft => Value << rhs,
            OperatorType.ShiftRight => Value >> rhs,
            OperatorType.Divide => Value / rhs,
            OperatorType.Modulo => Value % rhs,
            OperatorType.Multiply => Value * rhs,
            OperatorType.Power => Mathf.Pow(Value, rhs),
            OperatorType.Plus => Value + rhs,
            OperatorType.Minus => Value - rhs,
            _ => base.BinaryOperation(@operator, rhs),
        };
    }

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.BitwiseNot => ~Value,
        OperatorType.Minus => -Value,
        OperatorType.Plus => +Value,
        OperatorType.Decrement => prefix ? --Value : Value--,
        OperatorType.Increment => prefix ? ++Value : Value++,
        _ => base.UnaryOperation(@operator, prefix),
    };

    public override bool ToBool() => Value != 0;
    public override float ToF32() => Value;
    public override int ToI32() => Value;
    public override Vector2 ToVec2() => new(Value, Value);

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}

public class F32(float value) : Object<float>(value) {
    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.LessThan => Value < rhs,
        OperatorType.LessThanOrEqual => Value <= rhs,
        OperatorType.GreaterThan => Value > rhs,
        OperatorType.GreaterThanOrEqual => Value >= rhs,
        OperatorType.Divide => Value / rhs,
        OperatorType.Modulo => Value % rhs,
        OperatorType.Multiply => Value * rhs,
        OperatorType.Power => Mathf.Pow(Value, rhs),
        OperatorType.Plus => Value + rhs,
        OperatorType.Minus => Value - rhs,
        _ => base.BinaryOperation(@operator, rhs),
    };

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Plus => +Value,
        OperatorType.Minus => -Value,
        OperatorType.Decrement => prefix ? --Value : Value--,
        OperatorType.Increment => prefix ? ++Value : Value++,
        _ => base.UnaryOperation(@operator, prefix),
    };

    public override bool ToBool() => Value != 0;
    public override float ToF32() => Value;
    public override int ToI32() => (int)Value;
    public override Vector2 ToVec2() => new(Value, Value);

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}

public class Str : Object<string> {
    private readonly ReadOnlyProperty _length;
    public Str(string value)
    : base(value) {
        _length = new(this, "length", (_, key) => {
            Debug.Assert(key.Equals("length"));
            return Value.Length;
        });
    }
    public override Property GetProperty(object key) => key switch {
        int idx => new ReadOnlyProperty(this, idx, (_, idx) => Value[(int)idx]),
        "length" => _length,
        _ => base.GetProperty(key),
    };

    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.Plus => Value + rhs.ToString(),
        _ => base.BinaryOperation(@operator, rhs)
    };

    public override IEnumerator<Object> GetEnumerator() {
        foreach (char c in Value)
            yield return c;
    }

    public override bool ToBool() => !string.IsNullOrEmpty(Value);

    public override Object Copy(bool shallow = true, params object[] keys) => shallow ? Value : new string(Value);
}

public class Vec2 : Object<Vector2> {
    private readonly SetGetProperty _x;
    private readonly SetGetProperty _y;

    private readonly Callable _normalizeFn;
    private readonly ReadOnlyValueProperty _normalize;

    public Vec2(Vector2 value)
    : base(value) {
        _x = new(this, "x",
        (_, key) => {
            Debug.Assert(key.Equals("x"));
            return Value.X;
        },
        (_, key, value) => {
            Debug.Assert(key.Equals("x"));
            Value = new(value, Value.Y);
        });

        _y = new(this, "y",
        (_, key) => {
            Debug.Assert(key.Equals("y"));
            return Value.X;
        },
        (_, key, value) => {
            Debug.Assert(key.Equals("y"));
            Value = new(Value.X, value);
        });

        _normalizeFn = new(_ => Value = Value.Normalized());
        _normalize = new(this, "normalize", _normalizeFn);
    }

    public override Property GetProperty(object key) => key switch {
        "x" => _x,
        "y" => _y,
        "length" => new ValueProperty(this, key, Value.Length()),
        "normalized" => new ValueProperty(this, key, Value.Normalized()),
        "normalize" => _normalize,
        _ => base.GetProperty(key)
    };

    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.LessThan => Value < rhs,
        OperatorType.LessThanOrEqual => Value <= rhs,
        OperatorType.GreaterThan => Value > rhs,
        OperatorType.GreaterThanOrEqual => Value >= rhs,
        OperatorType.Plus => Value + rhs,
        OperatorType.Minus => Value - rhs,
        OperatorType.Multiply when rhs is I32 i => Value * (float)i,
        OperatorType.Multiply when rhs is F32 f => Value * (float)f,
        OperatorType.Multiply => Value * (Vector2)rhs,
        OperatorType.Divide when rhs is I32 i => Value / (float)i,
        OperatorType.Divide when rhs is F32 f => Value / (float)f,
        OperatorType.Divide => Value / (Vector2)rhs,
        OperatorType.Modulo when rhs is I32 i => Value % (float)i,
        OperatorType.Modulo when rhs is F32 f => Value % (float)f,
        OperatorType.Modulo => Value % (Vector2)rhs,
        _ => base.BinaryOperation(@operator, rhs)
    };

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Plus => Value,
        OperatorType.Minus => -Value,
        _ => base.UnaryOperation(@operator, prefix)
    };

    public override bool ToBool() => Value != Vector2.Zero;
    public override Vector2 ToVec2() => Value;

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}

public class Unset() : Object(null) {
    public override bool ToBool() => false;
    public override float ToF32() => 0f;
    public override int ToI32() => 0;
    public override Vector2 ToVec2() => Vector2.Zero;
    public override string ToString() => "unset";

    public override Object Copy(bool shallow = true, params object[] keys) => this;
}

public delegate Object CallFunction(params Object[] args);
public delegate void CallFunctionImplicitNullReturn(params Object[] args);
public class Callable(CallFunction value) : Object<CallFunction>(value) {
    public Callable(CallFunctionImplicitNullReturn value)
    : this((args) => {
        value(args);
        return new Unset();
    }) { }

    public override Object Call(params Object[] args) {
        return Value(args);
    }

    public override Object Copy(bool shallow = true, params object[] keys) => this;
}