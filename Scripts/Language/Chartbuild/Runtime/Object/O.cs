using System.Collections;
using System.Collections.Generic;
using Godot;

namespace PCE.Chartbuild.Runtime;

public abstract class Property(O @this, object key)/*  : O */ {
    protected readonly O @this = @this;
    protected readonly object key = key;
    protected abstract O Getter();
    protected abstract void Setter(O value);

    public O Get() => Getter();
    public void Set(O value) {
        Setter(value);
    }
}

public class ValueProperty(O @this, object key, O value) : Property(@this, key) {
    private O value = value;

    protected override O Getter() => value;

    protected override void Setter(O value) => this.value = value;
}

public abstract class BinaryOperationHandler(O lhs) {
    protected readonly O lhs = lhs;
    public abstract O BinaryOperation(OperatorType @operator, O rhs);
}

public abstract class UnaryOperatorHandler(O @this) {
    protected readonly O @this = @this;
    public abstract O UnaryOperation(OperatorType @operator);
}

public abstract class CallHandler(O @this) {
    protected readonly O @this = @this;
    public abstract O Call(params O[] args);
}

public abstract class EnumeratorGetHandler(O @this) : IEnumerable<O> {
    protected readonly O @this = @this;

    public abstract IEnumerator<O> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public abstract class O(object nativeValue) {
    public object nativeValue = nativeValue;
    public O this[object key] { get => GetProperty(key).Get(); set => GetProperty(key).Set(value); }
    public BinaryOperationHandler binaryOperationHandler;
    public UnaryOperatorHandler unaryOperatorHandler;
    public CallHandler callHandler;
    public EnumeratorGetHandler enumeratorGetHandler;

    public abstract Property GetProperty(object key);

    public virtual List<O> ToList() => throw new System.InvalidCastException();
    public virtual bool ToBool() => throw new System.InvalidCastException();
    public virtual int ToI32() => throw new System.InvalidCastException();
    public virtual float ToF32() => throw new System.InvalidCastException();
    public virtual Vector2 ToVec2() => throw new System.InvalidCastException();

    public KeyNotFoundException KeyNotFound(object key) => new($"unknown property on {GetType()}: {key}");

    public override string ToString() => nativeValue.ToString();
    public override int GetHashCode() => nativeValue.GetHashCode();
}

public abstract class O<T>(T value) : O(value) {
    private T _value = value;
    public T Value {
        get => _value;
        set {
            _value = value;
            nativeValue = value;
        }
    }

    public O() : this(default) {}
}

public class KWObject() : O(null) {
    private readonly Dictionary<object, Property> properties = [];
    public override Property GetProperty(object key) => properties[key];

    public void AddProperty(object key, Property property) {
        properties[key] = property;
    }
}

public class OArray : O<List<O>> {
    // default for a class is null
    public OArray() : base([]) { }

    public override Property GetProperty(object key) => key switch {
        int idx => new ValueProperty(this, idx, Value[idx]),
        "length" => throw new System.NotImplementedException(),
        "push" => throw new System.NotImplementedException(),
        "pop" => throw new System.NotImplementedException(),
        _ => throw KeyNotFound(key)
    };

    public override List<O> ToList() => Value;
    public override bool ToBool() => Value is not null && Value.Count > 0;
    public override string ToString() => $"[{string.Join(", ", Value)}]";
}

public class B : O<bool> {
    public override Property GetProperty(object key) {
        throw KeyNotFound(key);
    }

    public override bool ToBool() => Value;
    public override float ToF32() => Value ? 0f : 1f;
    public override int ToI32() => Value ? 0 : 1;
    public override Vector2 ToVec2() => Value ? Vector2.Zero : Vector2.One;
}