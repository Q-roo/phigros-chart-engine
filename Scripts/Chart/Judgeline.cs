using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Judgeline : Line2D, ICBExposeable {
    public TransformGroup parent;
    private float _size;
    public float Size {
        get => _size;
        set {
            _size = value;
            Points = [
                new (-value / 2f, 0),
                new (value / 2f, 0)
            ];
        }
    }
    public readonly StringName name;
    public float bpm;

    public Judgeline(StringName name, float bpm, float size) {
        Size = size;
        Width = 5;
        this.bpm = bpm;
        this.name = name;
        Name = name;
    }

    public Judgeline()
    : this(ChartContext.GetJudgelineName(), 120, 4000) {}

    public NativeObject ToObject() {
        return new(
            this,
            key => key switch {
                "size" => new F32(Size),
                "position" => new Vec2(Position),
                _ => throw new KeyNotFoundException()
            },
            (key, value) => {
                switch (key) {
                    case "size":
                        Size = value.ToF32().value;
                        break;
                    case "position":
                        Position = value.ToVec2().value;
                        break;
                    default:
                        throw new KeyNotFoundException();
                }
            }
        );
    }

    public override int GetHashCode() {
        return name.GetHashCode();
    }
    public override string ToString() {
        return $"judgeline({name} ({bpm})";
    }
}