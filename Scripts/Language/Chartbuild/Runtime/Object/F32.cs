using Godot;

namespace PCE.Chartbuild.Runtime;

public class F32(float value) : Object<float>(value) {
    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.LessThan => Value < rhs,
        OperatorType.LessThanOrEqual => Value <= rhs,
        OperatorType.GreaterThan => Value > rhs,
        OperatorType.GreaterThanOrEqual => Value >= rhs,
        OperatorType.Divide => Value / rhs,
        OperatorType.Modulo => Value % rhs,
        OperatorType.Multiply => Value * rhs,
        OperatorType.Power => Mathf.Pow(Value, rhs),
        OperatorType.Plus => Value + rhs,
        OperatorType.Minus => Value - rhs,
        _ => base.BinaryOperation(@operator, rhs),
    };

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Plus => +Value,
        OperatorType.Minus => -Value,
        OperatorType.Decrement => prefix ? --Value : Value--,
        OperatorType.Increment => prefix ? ++Value : Value++,
        _ => base.UnaryOperation(@operator, prefix),
    };

    public override bool ToBool() => Value != 0;
    public override float ToF32() => Value;
    public override int ToI32() => (int)Value;
    public override Vector2 ToVec2() => new(Value, Value);

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}