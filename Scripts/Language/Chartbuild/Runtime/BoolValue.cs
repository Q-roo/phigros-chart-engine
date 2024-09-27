using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class BoolValue(bool value) : ICBValue {
    public BaseType Type => new BoolType();
    public bool IsReference => false;

    private bool value = value;

    public BoolValue()
    : this(false) { }

    public object GetValue() => value;
    public ErrorType SetValue(object value) {
        if (value is bool b) {
            this.value = b;
            return ErrorType.NoError;
        }

        return ErrorType.InvalidType;
    }

    Either<ICBValue, ErrorType> ICBValue.Clone() => Clone();
    public BoolValue Clone() => new(value);

    public static implicit operator bool(BoolValue value) => value.value;
    public static implicit operator BoolValue(bool value) => new(value);

    public static BoolValue operator !(BoolValue value) => new(!value.value);

    public static BoolValue operator ==(BoolValue lhs, object rhs) => lhs.value.Equals(rhs) || rhs is BoolValue r && lhs.value == r.value;
    public static BoolValue operator !=(BoolValue lhs, object rhs) => !(lhs == rhs);
    public override bool Equals(object obj) => value.Equals(obj) || obj is BoolValue v && value == v.value;
    public override int GetHashCode() => value.GetHashCode();

    public override string ToString() => value.ToString();

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        bool value = this.value;
        return rhs.TryCastThen<BoolValue, ICBValue>(@bool => @operator switch {
            TokenType.Equal => new BoolValue(@bool.value == value),
            TokenType.NotEqual => new BoolValue(@bool.value != value),
            _ => ErrorType.NotSupported
        });
    }

    public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        return @operator switch {
            TokenType.Equal => new BoolValue(Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!Equals(rhs)),
            _ => throw new UnreachableException()
        };
    }
}