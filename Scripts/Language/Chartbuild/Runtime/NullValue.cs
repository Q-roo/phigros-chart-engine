using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class NullValue : ICBValue {
    public BaseType Type => new NullType();

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return @operator switch { 
            TokenType.Equal => new BoolValue(Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!Equals(rhs)),
            _ => ErrorType.NullValue,
        };
    }

    public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        return @operator switch { 
            TokenType.Equal => new BoolValue(Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!Equals(rhs)),
            _ => throw new UnreachableException(),
        };
    }

    public Either<ICBValue, ErrorType> Call(params ICBValue[] args) => ErrorType.NullValue;

    public object GetValue() {
        return null;
    }

    public ErrorType SetValue(object value) {
        return ErrorType.NullValue;
    }

    public override bool Equals(object obj) => obj is null || obj is NullValue || obj is ICBValue v && v.GetValue() is null;

    public override int GetHashCode() => 0;
}