using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;

namespace PCE.Chartbuild.Runtime;

public class Vec2(Vector2 value) : Object {
    public Vector2 value = value;

    public override Object this[object key] {
        get => key switch {
            "x" => new F32(value.X) { parentKey = "x", parentObject = this },
            "y" => new F32(value.Y) { parentKey = "y", parentObject = this },
            "length" => new F32(value.Length()),
            "normalized" => new Vec2(value.Normalized()),
            "normalize" => new NativeFunction(_ => value = value.Normalized()),
            _ => throw KeyNotFound(key)
        }; set {
            switch (key) {
                case "x":
                    this.value.X = value.ToF32().value;
                    break;
                case "y":
                    this.value.Y = value.ToF32().value;
                    break;
                case "length":
                case "normalize":
                case "normalized":
                    throw ReadOnlyProperty(key);
                default:
                    throw KeyNotFound(key);
            }
        }
    }

    public override object Value => value;

    public override Object Call(params Object[] args) {
        throw NotCallable();
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return new Vec2(value);
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        if (@operator == OperatorType.Equal)
            return new Bool(value.Equals(rhs.Value));
        if (@operator == OperatorType.Equal)
            return new Bool(!value.Equals(rhs.Value));

        // vector2 has operators for `v2 op f`
        if (rhs is Vec2 vec2) {
            Vector2 value = vec2.value;
            return @operator switch {
                OperatorType.LessThan => new Bool(this.value < value),
                OperatorType.LessThanOrEqual => new Bool(this.value <= value),
                OperatorType.GreaterThan => new Bool(this.value > value),
                OperatorType.GreaterThanOrEqual => new Bool(this.value >= value),
                OperatorType.Plus => new Vec2(this.value + value),
                OperatorType.Minus => new Vec2(this.value - value),
                OperatorType.Multiply => new Vec2(this.value * value),
                OperatorType.Divide => new Vec2(this.value / value),
                OperatorType.Modulo => new Vec2(this.value % value),
                _ => throw NotSupportedOperator(@operator)
            };

        } else {
            float value = rhs.ToF32().value;
            return @operator switch {
                OperatorType.Multiply => new Vec2(this.value * value),
                OperatorType.Divide => new Vec2(this.value / value),
                OperatorType.Modulo => new Vec2(this.value % value),
                _ => throw NotSupportedOperator(@operator)
            };
        }
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return @operator switch {
            OperatorType.Minus => new Vec2(-value),
            _ => throw NotSupportedOperator(@operator)
        };
    }

    public override IEnumerator<Object> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() {
        return value.ToString();
    }

    public override Vec2 ToVec2() {
        return this;
    }

    public override Bool ToBool() {
        return new(value != Vector2.Zero);
    }
}