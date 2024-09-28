using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace PCE.Chartbuild.Runtime;

public enum ValueType {
    Unset,
    Str,
    I32,
    F32,
    Bool,
    Callable,
    Array,
    Object
}

public interface IObjectPropertyDescriptor {
    public void Set(ObjectValue value);
    public ObjectValue Get();
}

public class DefaultObjectPropertyDescriptor(ObjectValue value) : IObjectPropertyDescriptor {
    public ObjectValue value = value;
    public virtual ObjectValue Get() => value;
    public virtual void Set(ObjectValue value) => this.value = value;
}

public class FunctionalObjectPropertyDescriptor(Func<ObjectValue> getter, Action<ObjectValue> setter) : IObjectPropertyDescriptor {
    private readonly Func<ObjectValue> getter = getter;
    private readonly Action<ObjectValue> setter = setter;

    public FunctionalObjectPropertyDescriptor(Func<ObjectValue> getter)
    : this(getter, value => throw new InvalidOperationException("cannot set a read-only property")) { }

    public void Set(ObjectValue value) => setter(value);
    public ObjectValue Get() => getter();
}

public class ObjectValue {
    public ValueType Type { get; init; }
    public object Value { get; private set; }

    public readonly Dictionary<object, IObjectPropertyDescriptor> members = [];

    public ObjectValue(object value) {
        if (value is ObjectValue objectValue)
            value = objectValue.Value;

        this.Value = value;

        Type = value switch {
            null => ValueType.Unset,
            string => ValueType.Str,
            int => ValueType.I32,
            double => ValueType.F32,
            bool => ValueType.Bool,
            Func<CBObject[], CBObject> => ValueType.Callable,
            _ => ValueType.Object,
        };
    }

    public ObjectValue()
    : this(null) { }

    public string AsString() => Type switch {
        ValueType.Unset => "unset",
        _ => $"{Value}"
    };

    public int AsInt() => Type switch {
        ValueType.Unset => 0,
        ValueType.I32 => (int)Value,
        ValueType.F32 => (int)(double)Value,
        ValueType.Bool => (bool)Value ? 1 : 0,
        _ => throw new UnreachableException()
    };

    public double AsDouble() => Type switch {
        ValueType.Unset => 0,
        ValueType.I32 => (int)Value,
        ValueType.F32 => (double)Value,
        ValueType.Bool => (bool)Value ? 1 : 0,
        _ => throw new UnreachableException()
    };

    public bool AsBool() => Type switch {
        ValueType.Unset => false,
        ValueType.Str => !string.IsNullOrEmpty((string)Value),
        ValueType.I32 => (int)Value != 0,
        ValueType.F32 => (double)Value != 0,
        ValueType.Bool => (bool)Value,
        _ => throw new UnreachableException()
    };

    public ObjectValue Cast(ValueType type) => type switch {
        ValueType.Unset => new(null),
        ValueType.Str => new(AsString()),
        ValueType.I32 => new(AsInt()),
        ValueType.F32 => new(AsDouble()),
        ValueType.Bool => new(AsBool()),
        ValueType.Callable => new(AsCallable()),
        ValueType.Object => new(Value),
        _ => throw new UnreachableException()
    };

    public Func<CBObject[], CBObject> AsCallable() => Type switch {
        ValueType.Callable => Call,
        _ => throw new UnreachableException()
    };

    public ObjectValue ExecuteBinaryOperator(TokenType @operator, ObjectValue rhs) {
        switch (Type) {
            case ValueType.Unset:
                throw new NullReferenceException();
            case ValueType.Str: {
                string a = AsString();
                string b = rhs.AsString();
                return @operator switch {
                    TokenType.Equal => new(a == b),
                    TokenType.NotEqual => new(a != b),
                    TokenType.Plus => new(a + b),
                    _ => throw new NotSupportedException()
                };
            }
            case ValueType.I32: {
                if (rhs.Type == ValueType.F32)
                    return new ObjectValue(AsDouble()).ExecuteBinaryOperator(@operator, rhs);

                int a = AsInt();
                int b = rhs.AsInt();
                return @operator switch {
                    TokenType.Equal => new(a == b),
                    TokenType.NotEqual => new(a != b),
                    TokenType.LessThan => new(a < b),
                    TokenType.GreaterThan => new(a > b),
                    TokenType.LessThanOrEqual => new(a <= b),
                    TokenType.GreaterThanOrEqual => new(a >= b),
                    TokenType.BitwiseAnd => new(a & b),
                    TokenType.BitwiseOr => new(a | b),
                    TokenType.BitwiseXor => new(a ^ b),
                    TokenType.ShiftLeft => new(a << b),
                    TokenType.ShiftRight => new(a >> b),
                    TokenType.Plus => new(a + b),
                    TokenType.Minus => new(a - b),
                    TokenType.Multiply => new(a * b),
                    TokenType.Power => new((int)Mathf.Pow(a, b)),
                    TokenType.Divide => new(a / b),
                    TokenType.Modulo => new(a % b),
                    _ => throw new NotSupportedException()
                };
            }
            case ValueType.F32: {
                double a = AsDouble();
                double b = rhs.AsDouble();
                return @operator switch {
                    TokenType.Equal => new(a == b),
                    TokenType.NotEqual => new(a != b),
                    TokenType.LessThan => new(a < b),
                    TokenType.GreaterThan => new(a > b),
                    TokenType.LessThanOrEqual => new(a <= b),
                    TokenType.GreaterThanOrEqual => new(a >= b),
                    TokenType.Plus => new(a + b),
                    TokenType.Minus => new(a - b),
                    TokenType.Multiply => new(a * b),
                    TokenType.Power => new((int)Mathf.Pow(a, b)),
                    TokenType.Divide => new(a / b),
                    TokenType.Modulo => new(a % b),
                    _ => throw new NotSupportedException()
                };
            }
            case ValueType.Bool:
                if (rhs.Type == ValueType.I32)
                    return new ObjectValue(AsInt()).ExecuteBinaryOperator(@operator, rhs);

                break;
            case ValueType.Callable:
            case ValueType.Object:
                throw new NotSupportedException();
        }

        throw new UnreachableException();
    }

    public ObjectValue ExecuteUnaryOperator(TokenType @operator) {
        switch (Type) {
            case ValueType.Unset:
                throw new NullReferenceException();
            case ValueType.I32: {
                int v = AsInt();
                switch (@operator) {
                    case TokenType.BitwiseNot:
                        return new(~v);
                    case TokenType.Plus:
                        return new(+v);
                    case TokenType.Minus:
                        return new(-v);
                    case TokenType.Increment:
                        Value = v + 1;
                        return new(v++);
                    case TokenType.Decrement:
                        Value = v - 1;
                        return new(v--);
                    default:
                        throw new NotSupportedException();
                }
            }
            case ValueType.F32: {
                double v = AsDouble();
                switch (@operator) {
                    case TokenType.Plus:
                        return new(+v);
                    case TokenType.Minus:
                        return new(-v);
                    case TokenType.Increment:
                        Value = v + 1;
                        return new(v++);
                    case TokenType.Decrement:
                        Value = v - 1;
                        return new(v--);
                    default:
                        throw new NotSupportedException();
                }
            }
            case ValueType.Bool: {
                bool v = AsBool();
                return @operator switch {
                    TokenType.Not => new(!v),
                    _ => throw new NotSupportedException(),
                };
            }
            case ValueType.Str:
            case ValueType.Callable:
            case ValueType.Object:
                throw new NotSupportedException();
        }
        throw new UnreachableException();
    }

    public virtual CBObject Call(params CBObject[] args) {
        Func<CBObject[], CBObject> callable = Value as Func<CBObject[], CBObject>;
        return callable(args);
    }

    public override string ToString() {
        return AsString();
    }

    public override bool Equals(object obj) {
        return Value.Equals(obj);
    }

    public override int GetHashCode() {
        return Value.GetHashCode();
    }
}

public class ObjectValueArray : ObjectValue {
    private readonly List<ObjectValue> values;
    public ObjectValueArray(List<ObjectValue> content)
    : base(content) {
        Type = ValueType.Array;
        values = content;

        members["length"] = new FunctionalObjectPropertyDescriptor(() => new(values.Count));

        // TODO: add, remove, ...etc

        for (int i = 0; i < content.Count; i++)
            members[i] = new DefaultObjectPropertyDescriptor(content[i]);
    }

    public override string ToString() {
        return $"[{string.Join(", ", values)}]";
    }
}

public class ObjectValueClosure : ObjectValue {
    private readonly UnsafeVM vm;

    public ObjectValueClosure(UnsafeVM vm)
    : base(vm) {
        this.vm = vm;
        Type = ValueType.Callable;
    }

    public override CBObject Call(params CBObject[] args) {
        return new(vm.Run(args));
    }
}

public class CBObject {
    private ObjectValue value;
    // will be set once after the first assignment
    public ValueType InitalType { get; private set; }
    public ValueType CurrentType => value.Type;
    private bool initalized;

    public CBObject(ObjectValue value) {
        this.value = value;
        if (value.Type != ValueType.Unset) {
            initalized = true;
            InitalType = this.value.Type;
        }
    }
    public CBObject(object value)
    : this(new(value)) { }
    public CBObject()
    : this(new(null)) { }

    public CBObject ShallowCopy() => new(new(value));

    public void SetValue(ObjectValue value) {
        if (!initalized) {
            InitalType = value.Type;
            initalized = true;
        }

        this.value = value.Cast(InitalType);
    }

    public ObjectValue GetValue() => value;
}