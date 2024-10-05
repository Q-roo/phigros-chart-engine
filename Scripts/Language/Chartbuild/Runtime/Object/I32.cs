using System.Collections.Generic;
using Godot;

namespace PCE.Chartbuild.Runtime;

public class I32(int value) : Object {
    public int value = value;
    public override object Value => value;

    public override Object this[object key] { get => throw KeyNotFound(key); set => throw KeyNotFound(key); }

    public override Object SetValue(Object value) {
        this.value = value.ToI32().value;
        return value;
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new I32(value);
    }

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        if (rhs is F32)
            return new F32(this.value).ExecuteBinary(@operator, rhs);

        if (@operator == OperatorType.Equal)
            return new Bool(this.value.Equals(rhs.Value));
        else if (@operator == OperatorType.NotEqual)
            return new Bool(!this.value.Equals(rhs.Value));

        int value = rhs.ToI32().value;

        return @operator switch {
            OperatorType.LessThan => new Bool(this.value < value),
            OperatorType.LessThanOrEqual => new Bool(this.value <= value),
            OperatorType.GreaterThan => new Bool(this.value > value),
            OperatorType.GreaterThanOrEqual => new Bool(this.value >= value),
            OperatorType.BitwiseAnd => new I32(this.value & value),
            OperatorType.BitwiseOr => new I32(this.value | value),
            OperatorType.BitwiseXor => new I32(this.value ^ value),
            OperatorType.ShiftLeft => new I32(this.value << value),
            OperatorType.ShiftRight => new I32(this.value >> value),
            OperatorType.Plus => new I32(this.value + value),
            OperatorType.Minus => new I32(this.value - value),
            OperatorType.Multiply => new I32(this.value * value),
            OperatorType.Power => new I32((int)Mathf.Pow(this.value, value)),
            OperatorType.Divide => new I32(this.value / value),
            OperatorType.Modulo => new I32(this.value % value),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return new I32(@operator switch {
            OperatorType.Increment => prefix ? ++value : value++,
            OperatorType.Decrement => prefix ? --value : value++,
            OperatorType.BitwiseNot => ~value,
            OperatorType.Plus => +value,
            OperatorType.Minus => -value,
            _ => throw NotSupportedOperator(@operator)
        });
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => value.ToString();

    public override Bool ToBool() {
        return new(value != 0);
    }

    public override F32 ToF32() {
        return new(value);
    }

    public override I32 ToI32() {
        return this;
    }
}