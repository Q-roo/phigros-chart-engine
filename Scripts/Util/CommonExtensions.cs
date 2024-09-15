using System;
using DotNext;

namespace PCE.Util;
public static class CommonExtensions
{
    public static Result<U, R> AndThen<T, U, R>(this Result<T, R> result, System.Func<T, U> func) where R : struct, Enum
    {
        return result ? func(result.Value) : new Result<U, R>(result.Error);
    }

    public static void AndThen<T, R>(this Result<T, R> result, Action<T> action) where R : struct, Enum
    {
        if (result)
            action(result.Value);
    }

    public static bool IsNumericType(this object o)
    {
        return Type.GetTypeCode(o.GetType()) switch
        {
            TypeCode.Byte or
            TypeCode.SByte or
            TypeCode.UInt16 or
            TypeCode.UInt32 or
            TypeCode.UInt64 or
            TypeCode.Int16 or
            TypeCode.Int32 or
            TypeCode.Int64 or
            TypeCode.Decimal or
            TypeCode.Double or
            TypeCode.Single => true,
            _ => false,
        };
    }
}