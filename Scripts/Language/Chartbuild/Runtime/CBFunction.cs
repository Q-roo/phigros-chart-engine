using System.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public abstract class CBFunction {
    public BaseType returnType;
    public List<BaseType> argumentTypes;
    public string[] argumentNames;
    public bool isLastParams;
    public BaseType Type => new FunctionType(returnType, isLastParams, [.. argumentTypes]);
    public bool Callable => true;
    public bool IsReference => true;
    public bool IsPureCallable => pure;
    public bool pure;

    public static Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
        return ErrorType.NotSupported;
    }

    public abstract Either<ICBValue, ErrorType> Call(params ICBValue[] args);

    public object GetValue() => this;

    public static ErrorType SetValue(object value) => ErrorType.SetConstant; // function values are immutable because I don't want to create a new error type
}