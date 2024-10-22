using Godot;

namespace PCE.Editor;

public partial class GetVariableGraphNode : GraphNode {
    private readonly LineEdit name = new();
    public GetVariableGraphNode() {
        Title = "get variable";
        name.PlaceholderText = "name";
        name.SizeFlagsVertical |= SizeFlags.Expand;
        AddChild(name);
        SetSlotEnabledRight(0, true);
        Resizable = true;
    }
}