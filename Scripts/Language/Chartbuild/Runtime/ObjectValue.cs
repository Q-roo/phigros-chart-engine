using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace PCE.Chartbuild.Runtime;

public class ObjectValue {
    public static ObjectValue Unset => new((object)null);

    public ValueType Type { get; init; }
    public virtual object Value { get; private set; }

    private readonly Dictionary<object, CBObject> members = [];

    public virtual CBObject GetMember(object key) => members[key];
    public virtual void SetMember(object key, CBObject value) => members[key] = value;

    public ObjectValue(ObjectValue @object) {
        Value = @object.Value;
        Type = @object.Type;
    }

    public ObjectValue(object value) {
        // both constructors are needed
        if (value is ObjectValue @object) {
            Type = @object.Type;
            Value = @object.Value;
            return;
        }

        Value = value;

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