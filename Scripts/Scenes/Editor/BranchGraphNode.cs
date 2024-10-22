using Godot;

namespace PCE.Editor;

public partial class BranchGraphNode : GraphNode {
    public BranchGraphNode() {
        Title = "branch";
        HBoxContainer container = new();
        container.AddChild(new Label() { Text = "condition", SizeFlagsHorizontal = SizeFlags.Expand });
        container.AddChild(new Label() { Text = "true", HorizontalAlignment = HorizontalAlignment.Right });
        AddChild(container);
        AddChild(new Label() { Text = "false", HorizontalAlignment = HorizontalAlignment.Right });

        SetSlotEnabledLeft(0, true);
        SetSlotEnabledRight(0, true);
        SetSlotEnabledRight(1, true);
    }
}