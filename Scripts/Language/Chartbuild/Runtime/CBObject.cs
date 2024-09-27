using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public enum ValueType {
    Unset,
    Str,
    I32,
    F32,
    Bool,
    Callable,
    Object
}

public class ObjectValue {
    public readonly ValueType type;
    public readonly object value;

    public ObjectValue(object value) {
        if (value is ObjectValue objectValue)
            value = objectValue.value;

        this.value = value;

        this.type = value switch {
            null => ValueType.Unset,
            string => ValueType.Str,
            int => ValueType.I32,
            double => ValueType.F32,
            bool => ValueType.Bool,
            Func<CBObject[], CBObject> => ValueType.Callable,
            _ => ValueType.Object,
        };
    }

    public string AsString() => type switch {
        ValueType.Unset => "unset",
        _ => $"{value}"
    };

    public int AsInt() => type switch {
        ValueType.Unset => 0,
        ValueType.I32 => (int)value,
        ValueType.F32 => (int)(double)value,
        ValueType.Bool => (bool)value ? 1 : 0,
        _ => throw new UnreachableException()
    };

    public double AsDouble() => type switch {
        ValueType.Unset => 0,
        ValueType.I32 => (int)value,
        ValueType.F32 => (double)value,
        ValueType.Bool => (bool)value ? 1 : 0,
        _ => throw new UnreachableException()
    };

    public bool AsBool() => type switch {
        ValueType.Unset => false,
        ValueType.Str => !string.IsNullOrEmpty((string)value),
        ValueType.I32 => (int)value != 0,
        ValueType.F32 => (double)value != 0,
        ValueType.Bool => (bool)value,
        _ => throw new UnreachableException()
    };

    public Func<CBObject[], CBObject> AsCallable() => type switch {
        ValueType.Callable => Call,
        _ => throw new UnreachableException()
    };

    public CBObject Call(params CBObject[] args) {
        throw new NotImplementedException();
    }
}

public class CBObject(/* ICBValue value */) /*: ICBValue, IEnumerableICBValue, ICallableICBValue */ {
    public ObjectValue value = new(null);
    // will be set once after the first assignment
    public ValueType InitalType { get; private set; } = ValueType.Unset;
    public ValueType CurrentType;

    public CBObject ExecuteBinaryOperator(TokenType @opertator, ObjectValue rhs) {
        throw new NotImplementedException();
    }
    // public ICBValue Value {get; private set;} = value;
    // public BaseType Type => Value.Type;
    // public bool IsReference => Value.IsReference;

    // public BaseType InnerType => (Value as IEnumerableICBValue).InnerType;

    // public bool IsPureCallable => (Value as ICallableICBValue).IsPureCallable;

    // public bool IsLastParams => (Value as ICallableICBValue).IsLastParams;

    // public BaseType ReturnType => (Value as ICallableICBValue).ReturnType;

    // public string[] ParameterNames => (Value as ICallableICBValue).ParameterNames;

    // public BaseType[] ParameterTypes => (Value as ICallableICBValue).ParameterTypes;

    // public CBObject()
    // : this(null) { }

    // public Either<ICBValue, ErrorType> Call(params ICBValue[] args) {
    //     return (Value as ICallableICBValue).Call(args);
    // }

    // public Either<ICBValue, ErrorType> GetMember(ICBValue memberName) => Value.GetMember(memberName);
    // public ICBValue GetMemberUnsafe(ICBValue memberName) => Value.GetMemberUnsafe(memberName);

    // public Either<ICBValue, ErrorType> Clone() => Value.Clone();

    // public Either<ICBValue, ErrorType> ExecuteBinaryOperator(TokenType @operator, ICBValue rhs) {
    //     return Value.ExecuteBinaryOperator(@operator, rhs);
    // }

    // public ICBValue ExecuteBinaryOperatorUnsafe(TokenType @operator, ICBValue rhs) {
    //     return Value.ExecuteBinaryOperatorUnsafe(@operator, rhs);
    // }

    // public IEnumerator<ICBValue> GetEnumerator() {
    //     return (Value as IEnumerableICBValue).GetEnumerator();
    // }

    // public object GetValue() {
    //     return Value;
    // }

    // public ErrorType SetValue(object value) {
    //     if (value is not ICBValue v)
    //         return ErrorType.InvalidType;

    //     this.Value = v;
    //     return ErrorType.NoError;
    // }

    // IEnumerator IEnumerable.GetEnumerator() {
    //     return GetEnumerator();
    // }
}