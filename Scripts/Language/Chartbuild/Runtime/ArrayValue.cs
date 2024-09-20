using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class ArrayValue : IEnumerableICBValue {
    public BaseType innerType;
    public BaseType Type => new ArrayType(innerType);
    public BaseType InnerType => innerType;
    public bool IsReference => true;

    public List<ICBValue> values = [];

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return @operator switch {
            TokenType.Equal => new BoolValue(values.Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!values.Equals(rhs)),
            _ => ErrorType.NotSupported
        };
    }

    public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        return @operator switch {
            TokenType.Equal => new BoolValue(values.Equals(rhs)),
            TokenType.NotEqual => new BoolValue(!values.Equals(rhs)),
            _ => throw new UnreachableException()
        };
    }

    public ErrorType AddMember(ICBValue value) {
        innerType ??= value.Type;
        if (!value.Type.CanBeAssignedTo(innerType))
            return ErrorType.InvalidType;

        values.Add(value);
        return ErrorType.NoError;
    }

    public Either<ICBValue, ErrorType> GetMember(ICBValue memberName) {
        return memberName.TryCastThen<I32Value, ICBValue>(i32 => {
            int idx = i32;
            if (idx < 0 || idx >= values.Count)
                return ErrorType.OutOfRange;

            return Either<ICBValue, ErrorType>.Left(values[idx]);
        });
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

    public IEnumerator<ICBValue> GetEnumerator() => values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}