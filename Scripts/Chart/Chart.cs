using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public CompatibilityLevel Platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");

    public CBObject ToCBObject() {
        return new(new FunctionalObjectValue(
            this,
            key => key switch {
                "platform" => new(Platform),
                "groups" => rootGroup.ToCBObject(),
                _ => throw new KeyNotFoundException()
            }
        ));
    }

    public override void _Ready() {
        AddChild(rootGroup);
    }
}