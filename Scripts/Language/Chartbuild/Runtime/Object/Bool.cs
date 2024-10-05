using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Bool(bool value) : Object {
    public bool value = value;
    public override object Value => value;

    public override Object this[object key] { get => throw KeyNotFound(key); set => throw KeyNotFound(key); }

    public override Object SetValue(Object value) {
        this.value = value.ToBool().value;
        return value;
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new Bool(value);
    }

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        // if rhs is a number, do math
        if (rhs is I32 || rhs is F32)
            return ToI32().ExecuteBinary(@operator, rhs);

        bool value = rhs.ToBool().value;

        return @operator switch {
            OperatorType.Or => new Bool(this.value || value),
            OperatorType.And => new Bool(this.value && value),
            // cases like true - false
            _ => ToI32().ExecuteBinary(@operator, rhs),
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return @operator switch {
            OperatorType.Not => new Bool(!value),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => value.ToString();

    public override Bool ToBool() {
        return this;
    }

    public override F32 ToF32() {
        return new(value ? 1 : 0);
    }

    public override I32 ToI32() {
        return new(value ? 1 : 0);
    }
}