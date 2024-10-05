using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public CompatibilityLevel Platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");

    public NativeObject ToObject() {
        return new(
            this,
            key => key switch {
                "platform" => new I32((int)Platform),
                "groups" => rootGroup.ToObject(),
                _ => throw new KeyNotFoundException()
            },
            (Key, value) => {

            }
        );
    }

    public override void _Ready() {
        AddChild(rootGroup);
    }
}