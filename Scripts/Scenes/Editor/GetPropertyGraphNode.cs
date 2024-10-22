using Godot;

namespace PCE.Editor;

public partial class GetPropertyGraphNode : GraphNode {
    public GetPropertyGraphNode() {
        Title = "get property";
        HBoxContainer container = new();
        container.AddChild(new Label() { Text = "object", SizeFlagsHorizontal = SizeFlags.Expand });
        container.AddChild(new Label() { Text = "value", HorizontalAlignment = HorizontalAlignment.Right });
        AddChild(container);
        AddChild(new Label() { Text = "property" });
        SetSlotEnabledLeft(0, true);
        SetSlotEnabledRight(0, true);
        SetSlotEnabledLeft(1, true);
    }
}