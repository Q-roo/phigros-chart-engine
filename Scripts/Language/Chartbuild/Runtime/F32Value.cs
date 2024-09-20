using LanguageExt;
using Godot;
using PCE.Util;
using System.Diagnostics;

namespace PCE.Chartbuild.Runtime;

public struct F32Value(double value) : ICBValue {
    public readonly BaseType Type => new F32Type();
    public readonly bool IsReference => true;

    public double value = value;

    public F32Value()
    : this(0) { }

    public readonly Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        F32Value lhs = this;
        return rhs.TryCastThen<F32Value, ICBValue>(f32 => @operator switch {
            TokenType.Plus => lhs + f32,
            TokenType.Minus => lhs - f32,
            TokenType.Multiply => lhs * f32,
            TokenType.Divide => (lhs / f32).MapLeft<ICBValue>(v => v),
            TokenType.Modulo => (lhs % f32).MapLeft<ICBValue>(v => v),
            TokenType.Power => Power(lhs, f32),
            TokenType.LessThan => lhs < f32,
            TokenType.LessThanOrEqual => lhs <= f32,
            TokenType.GreaterThan => lhs > f32,
            TokenType.GreaterThanOrEqual => lhs >= f32,
            _ => ErrorType.NotSupported
        });
    }

    public readonly ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        F32Value r = RHSAsF32Unsafe(rhs);
        return @operator switch {
            TokenType.Plus => this + r,
            TokenType.Minus => this - r,
            TokenType.Multiply => this * r,
            TokenType.Divide => DivUnsafe(this, r),
            TokenType.Modulo => ModUnsafe(this, r),
            TokenType.Power => Power(this, r),
            TokenType.LessThan => this < r,
            TokenType.LessThanOrEqual => this <= r,
            TokenType.GreaterThan => this > r,
            TokenType.GreaterThanOrEqual => this >= r,
            _ => throw new UnreachableException()
        };
    }

    public readonly object GetValue() {
        return value;
    }

    public readonly Either<F32Value, ErrorType> Clone() => new F32Value(value);

    public ErrorType SetValue(object value) {
        if (value.IsNumericType()) {
            this.value = (double)value;
            return ErrorType.NoError;
        }

        return ErrorType.InvalidType;
    }

    private static Either<F32Value, ErrorType> RHSAsF32(ICBValue v) {
        return v switch {
            F32Value f32 => f32,
            I32Value i32 => new F32Value(i32),
            _ => ErrorType.InvalidType
        };
    }

    private static F32Value RHSAsF32Unsafe(ICBValue v) {
        return v switch {
            F32Value f32 => f32,
            I32Value i32 => new F32Value(i32),
            _ => throw new UnreachableException()
        };
    }

    public static implicit operator double(F32Value value) => value.value;
    public static implicit operator F32Value(double value) => new(value);
    public static implicit operator F32Value(I32Value value) => new(value);

    public static F32Value DivUnsafe(F32Value lhs, F32Value rhs) => new(lhs.value / rhs.value);
    public static F32Value ModUnsafe(F32Value lhs, F32Value rhs) => new(lhs.value % rhs.value);

    public static F32Value Power(F32Value lhs, F32Value rhs) => new(Mathf.Pow(lhs.value, rhs.value));
    public static F32Value operator +(F32Value value) => new(+value.value);
    public static F32Value operator -(F32Value value) => new(-value.value);
    public static F32Value operator ++(F32Value value) => new(++value.value);
    public static F32Value operator --(F32Value value) => new(--value.value);

    public static F32Value operator +(F32Value lhs, F32Value rhs) => new(lhs.value + rhs.value);
    public static F32Value operator -(F32Value lhs, F32Value rhs) => new(lhs.value - rhs.value);
    public static F32Value operator *(F32Value lhs, F32Value rhs) => new(lhs.value * rhs.value);
    public static Either<F32Value, ErrorType> operator /(F32Value lhs, F32Value rhs) => rhs.value != 0 ? new F32Value(lhs.value / rhs.value) : ErrorType.DividedByZero;
    public static Either<F32Value, ErrorType> operator %(F32Value lhs, F32Value rhs) => rhs.value != 0 ? new F32Value(lhs.value % rhs.value) : ErrorType.DividedByZero;

    public static BoolValue operator ==(F32Value lhs, F32Value rhs) => new(lhs.value == rhs.value);
    public static BoolValue operator !=(F32Value lhs, F32Value rhs) => new(lhs.value != rhs.value);
    public static BoolValue operator >(F32Value lhs, F32Value rhs) => new(lhs.value > rhs.value);
    public static BoolValue operator <(F32Value lhs, F32Value rhs) => new(lhs.value < rhs.value);
    public static BoolValue operator >=(F32Value lhs, F32Value rhs) => new(lhs.value >= rhs.value);
    public static BoolValue operator <=(F32Value lhs, F32Value rhs) => new(lhs.value <= rhs.value);

    public override readonly bool Equals(object obj) => value.Equals(obj)
    || (obj is I32Value vi && value == vi.value)
    || (obj is F32Value vf && value == vf.value);
    public override readonly int GetHashCode() => value.GetHashCode();
}