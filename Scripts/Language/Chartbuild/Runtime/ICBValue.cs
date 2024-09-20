using System;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public interface ICBValue {
    public BaseType Type { get; }
    public bool IsReference => false; // is reference (e.g. class in c#)

    public object GetValue();
    public ErrorType SetValue(object value);

    public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs);
    public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs);
    // TODO: prefix & postfix execute

    // nothing is also an ICBValue
    public Either<ICBValue, ErrorType> GetMember(ICBValue memberName) => ErrorType.MissingMember;

    public Either<ICBValue, ErrorType> Clone() => ErrorType.NotSupported;
}