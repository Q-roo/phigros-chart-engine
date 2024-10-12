using Godot;

namespace PCE.Chartbuild.Runtime;

public class I32(int value) : Object<int>(value) {
    public override Object BinaryOperation(OperatorType @operator, Object rhs) {
        if (rhs is F32)
            return new F32(Value).BinaryOperation(@operator, rhs);

        return @operator switch {
            OperatorType.LessThan => Value < rhs,
            OperatorType.LessThanOrEqual => Value <= rhs,
            OperatorType.GreaterThan => Value > rhs,
            OperatorType.GreaterThanOrEqual => Value >= rhs,
            OperatorType.BitwiseAnd => Value & rhs,
            OperatorType.BitwiseOr => Value | rhs,
            OperatorType.BitwiseXor => Value ^ rhs,
            OperatorType.ShiftLeft => Value << rhs,
            OperatorType.ShiftRight => Value >> rhs,
            OperatorType.Divide => Value / rhs,
            OperatorType.Modulo => Value % rhs,
            OperatorType.Multiply => Value * rhs,
            OperatorType.Power => Mathf.Pow(Value, rhs),
            OperatorType.Plus => Value + rhs,
            OperatorType.Minus => Value - rhs,
            _ => base.BinaryOperation(@operator, rhs),
        };
    }

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.BitwiseNot => ~Value,
        OperatorType.Minus => -Value,
        OperatorType.Plus => +Value,
        OperatorType.Decrement => prefix ? --Value : Value--,
        OperatorType.Increment => prefix ? ++Value : Value++,
        _ => base.UnaryOperation(@operator, prefix),
    };

    public override bool ToBool() => Value != 0;
    public override float ToF32() => Value;
    public override int ToI32() => Value;
    public override Vector2 ToVec2() => new(Value, Value);

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}