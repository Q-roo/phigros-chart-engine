using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public abstract class CBFunction {
    public BaseType returnType;
    public BaseType[] paremeterTypes;
    public string[] parameterNames;
    public bool isLastParams;
    public BaseType Type => new FunctionType(returnType, isLastParams, paremeterTypes);
    public static bool Callable => true;
    public static bool IsReference => true;
    public bool IsPureCallable => pure;
    public bool pure;

    public static Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return ErrorType.NotSupported;
    }

    public static ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
        throw new UnreachableException();
    }

    public abstract Either<ICBValue, ErrorType> Call(params ICBValue[] args);

    public object GetValue() => this;

    public static ErrorType SetValue(object value) => ErrorType.SetConstant; // function values are immutable because I don't want to create a new error type
}