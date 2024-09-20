using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class CBFunctionValue(CBFunction value) : ICallableICBValue {
    public BaseType ReturnType => value.returnType;
    public BaseType[] ParameterTypes => value.paremeterTypes;
    public string[] ParameterNames => value.parameterNames;
    public bool IsLastParams => value.isLastParams;
    public BaseType Type => value.Type;
    public bool IsReference => true;
    public bool IsPureCallable => value.pure;

    public CBFunction value = value;

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) => CBFunction.ExecuteBinaryOperator(@operator, rhs);
    public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) => CBFunction.ExecuteBinaryOperatorUnsafe(@operator, rhs);

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