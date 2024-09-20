using System;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public static class ICBValueExtensions {
    public static Either<T, ErrorType> TryCast<T>(this ICBValue value) where T : ICBValue {
        BaseType type = default(T).Type;
        if (value.Type.CanBeAssignedTo(type)) {
            return type.Constructor(value).Case switch {
                T v => v,
                ErrorType err => err,
                _ => throw new UnreachableException()
            };
        }

        return ErrorType.InvalidType;
    }

    static public Either<U, ErrorType> TryCastThen<T, U>(this ICBValue value, Func<T, Either<U, ErrorType>> callback) where T : ICBValue {
        return value.TryCast<T>().Case switch {
            T v => callback(v),
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }
}