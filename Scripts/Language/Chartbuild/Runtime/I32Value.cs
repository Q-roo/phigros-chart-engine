using System;
using LanguageExt;
using Godot;
using PCE.Util;

namespace PCE.Chartbuild.Runtime;

public struct I32Value(int value) : ICBValue {
    public readonly BaseType Type => new IdentifierType("i32");
    public readonly bool IsReference => true;

    public int value = value;

    public I32Value()
    : this(0) { }

    public readonly Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        if (@operator == TokenType.Equal)
            return new BoolValue(Equals(rhs));
        else if (@operator == TokenType.NotEqual)
            return new BoolValue(!Equals(rhs));

        if (rhs is I32Value r)
            return @operator switch {
                TokenType.Plus => this + r,
                TokenType.Minus => this - r,
                TokenType.Multiply => this * r,
                TokenType.Divide => (this / r).MapLeft<ICBValue>(v => v),
                TokenType.Modulo => (this % r).MapLeft<ICBValue>(v => v),
                TokenType.Power => Power(this, r),
                TokenType.ShiftLeft => this << r,
                TokenType.ShiftRight => this >> r,
                TokenType.BitwiseXor => this ^ r,
                TokenType.BitwiseAnd => this & r,
                TokenType.BitwiseOr => this | r,
                TokenType.LessThan => this < r,
                TokenType.LessThanOrEqual => this <= r,
                TokenType.GreaterThan => this > r,
                TokenType.GreaterThanOrEqual => this >= r,
                _ => ErrorType.NotSupported
            };
        else
            return ErrorType.InvalidType;
    }

    public readonly object GetValue() {
        return value;
    }

    public readonly Either<I32Value, ErrorType> Clone() => new I32Value(value);

    public ErrorType SetValue(object value) {
        if (value.IsNumericType()) {
            this.value = (int)value;
            return ErrorType.NoError;
        }

        return ErrorType.InvalidType;
    }

    public static implicit operator int(I32Value value) => value.value;
    public static implicit operator I32Value(int value) => new(value);

    public static I32Value Power(I32Value lhs, I32Value rhs) => new((int)Mathf.Pow(lhs.value, rhs.value));

    public static I32Value operator +(I32Value value) => new(+value.value);
    public static I32Value operator -(I32Value value) => new(-value.value);
    public static I32Value operator ~(I32Value value) => new(~value.value);
    public static I32Value operator ++(I32Value value) => new(++value.value);
    public static I32Value operator --(I32Value value) => new(--value.value);

    public static I32Value operator +(I32Value lhs, I32Value rhs) => new(lhs.value + rhs.value);
    public static I32Value operator -(I32Value lhs, I32Value rhs) => new(lhs.value - rhs.value);
    public static I32Value operator *(I32Value lhs, I32Value rhs) => new(lhs.value * rhs.value);
    public static Either<I32Value, ErrorType> operator /(I32Value lhs, I32Value rhs) => rhs.value != 0 ? new I32Value(lhs.value / rhs.value) : ErrorType.DividedByZero;
    public static Either<I32Value, ErrorType> operator %(I32Value lhs, I32Value rhs) => rhs.value != 0 ? new I32Value(lhs.value % rhs.value) : ErrorType.DividedByZero;
    // public static I32Value operator **(I32Value lhs, I32Value rhs) => new(lhs.value ** rhs.value);
    public static I32Value operator <<(I32Value lhs, I32Value rhs) => new(lhs.value << rhs.value);
    public static I32Value operator >>(I32Value lhs, I32Value rhs) => new(lhs.value >> rhs.value);
    public static I32Value operator |(I32Value lhs, I32Value rhs) => new(lhs.value | rhs.value);
    public static I32Value operator &(I32Value lhs, I32Value rhs) => new(lhs.value & rhs.value);
    public static I32Value operator ^(I32Value lhs, I32Value rhs) => new(lhs.value ^ rhs.value);

    public static BoolValue operator ==(I32Value lhs, I32Value rhs) => new(lhs.value == rhs.value);
    public static BoolValue operator !=(I32Value lhs, I32Value rhs) => new(lhs.value != rhs.value);
    public static BoolValue operator >(I32Value lhs, I32Value rhs) => new(lhs.value > rhs.value);
    public static BoolValue operator <(I32Value lhs, I32Value rhs) => new(lhs.value < rhs.value);
    public static BoolValue operator >=(I32Value lhs, I32Value rhs) => new(lhs.value >= rhs.value);
    public static BoolValue operator <=(I32Value lhs, I32Value rhs) => new(lhs.value <= rhs.value);

    public override readonly bool Equals(object obj) => value.Equals(obj) ||
    (obj is I32Value vi && value == vi.value) ||
    (obj is F32Value vf && value == vf.value);
    public override readonly int GetHashCode() => value.GetHashCode();
}