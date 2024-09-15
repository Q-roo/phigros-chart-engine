using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public interface ICBValue {
    public BaseType Type { get; }
    public bool Callable => false;
    public bool IsReference => false; // is reference (e.g. class in c#)
    public bool IsPureCallable => true;

    public object GetValue();
    public ErrorType SetValue(object value);

    // public Result<ICBValue, ErrorType> ExecuteOperator(ICBValue handler, ICBValue lhs, ICBValue rhs, out ICBValue result);
    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs);

    // nothing is also an ICBValue
    public Either<ICBValue, ErrorType> Call(params ICBValue[] args) => ErrorType.NotSupported;
    public Either<ICBValue, ErrorType> CallMember(ICBValue memberName, params ICBValue[] args) => ErrorType.MissingMember;
    public Either<ICBValue, ErrorType> GetMember(ICBValue memberName) => ErrorType.MissingMember;

    public Either<ICBValue, ErrorType> Clone() => ErrorType.NotSupported;
}