using System;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class NativeFunctionBinding(Func<ICBValue[], Either<ICBValue, ErrorType>> method) : CBFunction {
    public NativeFunctionBinding(Action<ICBValue[]> method)
    : this(args => {
        method(args);
        return new NullValue();
    }) {
        returnType = new NullType();
    }
    public override Either<ICBValue, ErrorType> Call(params ICBValue[] args) {
        return method(args);
    }
}