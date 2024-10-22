using Godot;

namespace PCE.Editor;

public partial class UnaryOperationGraphNode : GraphNode {
    private readonly OptionButton dropdown = new();
    public UnaryOperationGraphNode() {
        Title = "unary operation";
        dropdown.AddItem("+");
        dropdown.AddItem("-");
        dropdown.AddItem("++");
        dropdown.AddItem("--");
        dropdown.AddItem("!");
        dropdown.AddItem("~");
        AddChild(dropdown);
        SetSlotEnabledLeft(0, true);
        SetSlotEnabledRight(0, true);
    }
}