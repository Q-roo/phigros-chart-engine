using System.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class CBFunctionValue(CBFunction value) : ICBValue {
    public BaseType ReturnType => value.returnType;
    public List<BaseType> ArgumentTypes => value.argumentTypes;
    public bool IsLastParams => value.isLastParams;
    public BaseType Type => value.Type;
    public bool Callable => true;
    public bool IsReference => true;
    public bool IsPureCallable => value.pure;

    public CBFunction value = value;

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) => CBFunction.ExecuteBinaryOperator(@operator, rhs);
    public Either<ICBValue, ErrorType> Call(params ICBValue[] args) => value.Call(args);

    public object GetValue() => value;

    public ErrorType SetValue(object value) {
        if (value is CBFunction function) {
            this.value = function;
            return ErrorType.NoError;
        }

        return ErrorType.InvalidType;
    }
}