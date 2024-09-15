using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public struct StringValue(string value) : ICBValue
{
    public readonly BaseType Type => new IdentifierType("str");
    public readonly bool IsReference => true;

    private string value = value;

    public StringValue()
    : this(string.Empty) { }

    public readonly object GetValue() => value;
    public ErrorType SetValue(object value)
    {
        if (value is string str)
        {
            this.value = str;
            return ErrorType.NoError;
        }
        else return ErrorType.InvalidType;
    }

    public readonly Either<StringValue, ErrorType> Clone() => new StringValue(value); // TODO: let's see if this breaks anything

    public readonly Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs)
    {
        if (@operator == TokenType.Equal)
            return new BoolValue(Equals(rhs));
        else if (@operator == TokenType.NotEqual)
            return new BoolValue(!Equals(rhs));

        return @operator switch
        {
            TokenType.Plus => rhs is StringValue r ? value + r : ErrorType.InvalidType,
            _ => ErrorType.NotSupported
        };
    }

    public static implicit operator string(StringValue value) => value.value;
    public static implicit operator StringValue(string value) => new(value);

    public static StringValue operator +(StringValue lhs, StringValue rhs) => new(lhs.value + rhs.value);
    public static BoolValue operator ==(StringValue lhs, object rhs) => lhs.Equals(rhs);
    public static BoolValue operator !=(StringValue lhs, object rhs) => !(lhs == rhs);
    public override readonly bool Equals(object obj) => value.Equals(obj)
    || (obj is StringValue v && value == v);
    public override readonly int GetHashCode() => value.GetHashCode();
}