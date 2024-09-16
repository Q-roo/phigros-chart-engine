using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class NullValue : ICBValue {
    public BaseType Type => new IdentifierType("unset");

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return ErrorType.NullValue;
    }

    public Either<ICBValue, ErrorType> Call(params ICBValue[] args) => ErrorType.NullValue;

    public object GetValue() {
        return null;
    }

    public ErrorType SetValue(object value) {
        return ErrorType.NullValue;
    }
}