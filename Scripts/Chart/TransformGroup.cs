using System;
using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class TransformGroup : Node2D, ICBExposeable {
    public readonly HashSet<TransformGroup> subGroups = [];
    public readonly HashSet<Judgeline> judgelines = [];
    // the elements in subgroups and judgelines
    // since the order matters
    public readonly List<Node2D> childOrder = [];
    public TransformGroup parentGroup;

    public TransformGroup() {

    }

    public TransformGroup(StringName name)
    : this() {
        Name = name;
    }

    public TransformGroup AddSubGroup(TransformGroup subGroup) {
        subGroup.AttachTo(this);
        return subGroup;
    }

    public TransformGroup AddSubGroup(StringName name) {
        return AddSubGroup(new TransformGroup(name));
    }

    public Judgeline AddJudgeline(Judgeline judgeline) {
        judgeline.AttachTo(this);
        return judgeline;
    }

    public Node2D GetMember(NodePath nodePath) {
        return GetNodeOrNull<Node2D>(nodePath);
    }

    public override int GetHashCode() {
        return Name.GetHashCode();
    }

    public NativeObject ToObject() {
        return new NativeObjectBuilder(this)
        .AddCallable("add_subgroup", AddSubgroup_Binding)
        .AddCallable("add_judgeline", AddJudgeline_Binding)
        .SetFallbackGetter((@this) => key => {
            if (key is not string property)
                throw new KeyNotFoundException("this object only has string keys");
            return new ReadOnlyValueProperty(@this, key, GetMember(property) is ICBExposeable exposeable ? exposeable.ToObject() : new Unset());
        })
        .Build();
    }

    NativeObject AddSubgroup_Binding(params Chartbuild.Runtime.Object[] args) {
        return AddSubGroup(args[0].ToString()).ToObject();
    }

    NativeObject AddJudgeline_Binding(params Chartbuild.Runtime.Object[] args) {
        if (
            args.Length == 0
            || args[0] is not NativeObject native
            || native.NativeValue is not Judgeline judgeline
        )
            throw new ArgumentException("this method requires one judgeline instance");

        return AddJudgeline(judgeline).ToObject();
    }
}