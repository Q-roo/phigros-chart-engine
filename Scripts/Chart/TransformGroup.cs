using System.Collections.Generic;
using Godot;
using PCE.Chartbuild;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class TransformGroup(StringName name) : Node2D, ICBExposeable {
    public readonly HashSet<TransformGroup> subGroups;
    public readonly StringName name = name;

    public override void _Ready() {
        Name = name;
    }

    public void AddSubGroup(TransformGroup subGroup) {
        subGroups.Add(subGroup);
        AddChild(subGroup);
        subGroup.AddToGroup(name);
    }

    public void AddSubGroup(StringName name) {
        AddSubGroup(new TransformGroup(name));
    }

    public Node2D GetMember(NodePath nodePath) {
        return GetNode<Node2D>(nodePath);
    }

    public override int GetHashCode() {
        return name.GetHashCode();
    }

    public CBObject ToCBObject() {
        return new FunctionalCBObject(
            obj => {
                if (obj.Type != ValueType.Str)
                return new(ErrorType.InvalidType);

                return new(((ICBExposeable)GetMember(obj.AsString())).ToCBObject());
            }
        );
    }
}