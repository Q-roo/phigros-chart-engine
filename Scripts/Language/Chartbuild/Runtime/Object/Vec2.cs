using System.Diagnostics;
using Godot;

namespace PCE.Chartbuild.Runtime;

public class Vec2 : Object<Vector2> {
    private readonly SetGetProperty _x;
    private readonly SetGetProperty _y;

    private readonly Callable _normalizeFn;
    private readonly ReadOnlyValueProperty _normalize;

    public Vec2(Vector2 value)
    : base(value) {
        _x = new(this, "x",
        (_, key) => {
            Debug.Assert(key.Equals("x"));
            return Value.X;
        },
        (_, key, value) => {
            Debug.Assert(key.Equals("x"));
            Value = new(value, Value.Y);
        });

        _y = new(this, "y",
        (_, key) => {
            Debug.Assert(key.Equals("y"));
            return Value.X;
        },
        (_, key, value) => {
            Debug.Assert(key.Equals("y"));
            Value = new(Value.X, value);
        });

        _normalizeFn = new(_ => Value = Value.Normalized());
        _normalize = new(this, "normalize", _normalizeFn);
    }

    public override Property GetProperty(object key) => key switch {
        "x" => _x,
        "y" => _y,
        "length" => new ValueProperty(this, key, Value.Length()),
        "normalized" => new ValueProperty(this, key, Value.Normalized()),
        "normalize" => _normalize,
        _ => base.GetProperty(key)
    };

    public override Object BinaryOperation(OperatorType @operator, Object rhs) => @operator switch {
        OperatorType.LessThan => Value < rhs,
        OperatorType.LessThanOrEqual => Value <= rhs,
        OperatorType.GreaterThan => Value > rhs,
        OperatorType.GreaterThanOrEqual => Value >= rhs,
        OperatorType.Plus => Value + rhs,
        OperatorType.Minus => Value - rhs,
        OperatorType.Multiply when rhs is I32 i => Value * (float)i,
        OperatorType.Multiply when rhs is F32 f => Value * (float)f,
        OperatorType.Multiply => Value * (Vector2)rhs,
        OperatorType.Divide when rhs is I32 i => Value / (float)i,
        OperatorType.Divide when rhs is F32 f => Value / (float)f,
        OperatorType.Divide => Value / (Vector2)rhs,
        OperatorType.Modulo when rhs is I32 i => Value % (float)i,
        OperatorType.Modulo when rhs is F32 f => Value % (float)f,
        OperatorType.Modulo => Value % (Vector2)rhs,
        _ => base.BinaryOperation(@operator, rhs)
    };

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Plus => Value,
        OperatorType.Minus => -Value,
        _ => base.UnaryOperation(@operator, prefix)
    };

    public override bool ToBool() => Value != Vector2.Zero;
    public override Vector2 ToVec2() => Value;

    public override Object Copy(bool shallow = true, params object[] keys) => Value;
}