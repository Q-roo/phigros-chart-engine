using Godot;

namespace PCE.Chartbuild.Runtime;

public class Bool(bool value) : Object<bool>(value) {
    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.And => Value && rhs,
        OperatorType.Or => Value || rhs,
        _ => new I32(this).BinaryOperation(@operator, rhs),
    };

    public override bool ToBool() => Value;
    public override float ToF32() => Value ? 0f : 1f;
    public override int ToI32() => Value ? 0 : 1;
    public override Vector2 ToVec2() => Value ? Vector2.Zero : Vector2.One;

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}