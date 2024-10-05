using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Judgeline : Line2D, ICBExposeable {
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

    public Judgeline() {
        // TODO: get default size
        Size = 3000;
    }

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
}