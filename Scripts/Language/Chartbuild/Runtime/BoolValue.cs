using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public struct BoolValue(bool value) : ICBValue {
    public readonly BaseType Type => new BoolType();
    public readonly bool IsReference => false;

    private bool value = value;

    public BoolValue()
    : this(false) { }

    public readonly object GetValue() => value;
    public ErrorType SetValue(object value) {
        if (value is bool b) {
            this.value = b;
            return ErrorType.NoError;
        }

        return ErrorType.InvalidType;
    }

    public readonly Either<BoolValue, ErrorType> Clone() => new BoolValue(value);

    public static implicit operator bool(BoolValue value) => value.value;
    public static implicit operator BoolValue(bool value) => new(value);

    public static BoolValue operator !(BoolValue value) => new(!value.value);

    public static BoolValue operator ==(BoolValue lhs, object rhs) => lhs.value.Equals(rhs) || rhs is BoolValue r && lhs.value == r.value;
    public static BoolValue operator !=(BoolValue lhs, object rhs) => !(lhs == rhs);
    public override readonly bool Equals(object obj) => value.Equals(obj) || obj is BoolValue v && value == v.value;
    public override readonly int GetHashCode() => value.GetHashCode();

    public override readonly string ToString() => value.ToString();

    public readonly Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        bool value = this.value;
        return rhs.TryCastThen<BoolValue, ICBValue>(@bool => @operator switch {
            TokenType.Equal => new BoolValue(@bool.value == value),
            TokenType.NotEqual => new BoolValue(@bool.value != value),
            _ => ErrorType.NotSupported
        });
    }

    public readonly ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        return @operator switch {
            TokenType.Equal => new BoolValue(Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!Equals(rhs)),
            _ => throw new UnreachableException()
        };
    }
}