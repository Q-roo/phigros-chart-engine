using System.Collections.Generic;
using Godot;

namespace PCE.Chartbuild.Runtime;

public class F32(float value) : Object {
    public float value = value;

    public override Object this[object key] { get => throw KeyNotFound(key); set => throw KeyNotFound(key); }

    protected override Object RequestSetValue(Object value) {
        this.value = value.ToF32().value;
        return value;
    }

    public override object Value => value;

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new F32(value);
    }

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        if (@operator == OperatorType.Equal)
            return new Bool(this.value.Equals(rhs.Value));
        else if (@operator == OperatorType.Equal)
            return new Bool(!this.value.Equals(rhs.Value));

        float value = rhs.ToF32().value;

        return @operator switch {
            OperatorType.LessThan => new Bool(this.value < value),
            OperatorType.LessThanOrEqual => new Bool(this.value <= value),
            OperatorType.GreaterThan => new Bool(this.value > value),
            OperatorType.GreaterThanOrEqual => new Bool(this.value >= value),
            OperatorType.Plus => new F32(this.value + value),
            OperatorType.Minus => new F32(this.value - value),
            OperatorType.Multiply => new F32(this.value * value),
            OperatorType.Power => new F32(Mathf.Pow(this.value, value)),
            OperatorType.Divide => new F32(this.value / value),
            OperatorType.Modulo => new F32(this.value % value),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return @operator switch {
            OperatorType.Plus => new F32(+value),
            OperatorType.Minus => new F32(-value),
            OperatorType.Increment => new F32(prefix ? ++value : value++),
            OperatorType.Decrement => new F32(prefix ? --value : value--),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => value.ToString();

    public override Bool ToBool() {
        return new(value != 0);
    }

    public override F32 ToF32() {
        return this;
    }

    public override I32 ToI32() {
        return new((int)value);
    }
}