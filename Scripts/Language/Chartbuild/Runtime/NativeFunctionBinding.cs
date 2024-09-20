using System;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class NativeFunctionBinding(Func<ICBValue[], Either<ICBValue, ErrorType>> method) : CBFunction {
    public override Either<ICBValue, ErrorType> Call(params ICBValue[] args) {
        return method(args);
    }
}