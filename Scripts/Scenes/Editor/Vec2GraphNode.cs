using Godot;

namespace PCE.Editor;

public partial class Vec2GraphNode : GraphNode {
    public Vec2GraphNode() {
        Title = "vec2";
        AddChild(new Label() { Text = "x" });
        AddChild(new Label() { Text = "y" });
        SetSlotEnabledLeft(0, true);
        SetSlotEnabledRight(0, true);
        SetSlotEnabledLeft(1, true);
    }
}