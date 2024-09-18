using System.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class ArrayValue : ICBValue {
    public BaseType innerType;
    public BaseType Type => new ArrayType(innerType);
    public bool IsReference => true;

    public List<ICBValue> values = [];

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return @operator switch {
            TokenType.Equal => new BoolValue(values.Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!values.Equals(rhs)),
            _ => ErrorType.NotSupported
        };
    }

    public ErrorType AddMember(ICBValue value) {
        innerType ??= value.Type;
        if (value.Type != innerType)
            return ErrorType.InvalidType;

        values.Add(value);
        return ErrorType.NoError;
    }

    public Either<ICBValue, ErrorType> GetMember(ICBValue memberName) {
        if (memberName is not I32Value idx)
            return ErrorType.InvalidType;

        if (idx.value < 0 || idx.value >= values.Count)
            return ErrorType.OutOfRange;

        return Either<ICBValue, ErrorType>.Left(values[idx.value]);
    }

    public object GetValue() {
        return values;
    }

    public ErrorType SetValue(object value) {
        if (value is ArrayValue array && array.innerType == innerType) {
            values = array.values;
            return ErrorType.NoError;
        }

        return ErrorType.InvalidType;
    }

    public override bool Equals(object obj) => values == obj || obj is ArrayValue array && array.values == values;

    public override int GetHashCode() => values.GetHashCode();
}