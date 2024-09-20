using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public struct StringValue(string value) : ICBValue {
    public readonly BaseType Type => new StringType();
    public readonly bool IsReference => true;

    public string value = value;

    public StringValue()
    : this(string.Empty) { }

    public readonly object GetValue() => value;
    public ErrorType SetValue(object value) {
        if (value is string str) {
            this.value = str;
            return ErrorType.NoError;
        } else return ErrorType.InvalidType;
    }

    public readonly Either<StringValue, ErrorType> Clone() => new StringValue(value); // TODO: let's see if this breaks anything

    public readonly Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        StringValue lhs = this;
        return rhs.TryCastThen<StringValue, ICBValue>(str => @operator switch {
            TokenType.Equal => lhs == str,
            TokenType.NotEqual => lhs != str,
            TokenType.Plus => lhs + str,
            _ => ErrorType.NotSupported
        });
    }

    public readonly ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        if (@operator == TokenType.Equal)
            return new BoolValue(Equals(rhs));
        else if (@operator == TokenType.NotEqual)
            return new BoolValue(!Equals(rhs));

        return @operator switch {
            TokenType.Plus => value + (StringValue)rhs,
            _ => throw new UnreachableException()
        };
    }

    public static implicit operator string(StringValue value) => value.value;
    public static implicit operator StringValue(string value) => new(value);

    public static StringValue operator +(StringValue lhs, StringValue rhs) => new(lhs.value + rhs.value);
    public static BoolValue operator ==(StringValue lhs, object rhs) => lhs.Equals(rhs);
    public static BoolValue operator !=(StringValue lhs, object rhs) => !(lhs == rhs);
    public override readonly bool Equals(object obj) => value.Equals(obj) || (obj is StringValue v && value == v);
    public override readonly int GetHashCode() => value.GetHashCode();

    public override readonly string ToString() => value;
}