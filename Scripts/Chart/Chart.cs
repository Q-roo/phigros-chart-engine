using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public CompatibilityLevel platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");

    public CBObject ToCBObject() {
        return new CBObjectBuilder(this)
            .CreateInstance()
            .Addproperty("platform", new(() => new(platform)))
            .Addproperty("groups", new(() => rootGroup.ToCBObject().GetValue()))
            .Build();
    }

    public override void _Ready() {
        AddChild(rootGroup);
    }
}