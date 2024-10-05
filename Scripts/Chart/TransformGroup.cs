using System;
using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

using Object = Chartbuild.Runtime.Object;

public partial class TransformGroup(StringName name) : Node2D, ICBExposeable {
    public readonly HashSet<TransformGroup> subGroups = [];
    public readonly HashSet<Judgeline> judgelines = [];
    public readonly StringName name = name;

    public override void _Ready() {
        Name = name;
    }

    public TransformGroup AddSubGroup(TransformGroup subGroup) {
        subGroups.Add(subGroup);
        AddChild(subGroup);
        subGroup.AddToGroup(name);

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
        return GetNode<Node2D>(nodePath);
    }

    public override int GetHashCode() {
        return name.GetHashCode();
    }

    public NativeObject ToObject() {
        return new(
            this,
            key => {
                if (key is not string property)
                    throw new KeyNotFoundException("this object only has string keys");

                return property switch {
                    "add_subgroup" => new NativeFunction(AddSubgroup_Binding),
                    "add_judgeline" => new NativeFunction(AddJudgeline_Binding),
                    _ => GetMember(property) is ICBExposeable exposeable ? exposeable.ToObject() : new Unset()
                };
            },
            (Key, value) => {

            }
        );
    }

    NativeObject AddSubgroup_Binding(params Object[] args) {
        return AddSubGroup((string)args[0].Value).ToObject();
    }

    NativeObject AddJudgeline_Binding(params Object[] args) {
        if (
            args.Length == 0
            || args[0] is not NativeObject native
            || native.Value is not Judgeline judgeline
        )
        throw new ArgumentException("this method requires one judgeline instance");

        return AddJudgeline(judgeline).ToObject();
    }
}