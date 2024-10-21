using Godot;

namespace PCE.Editor;

public partial class BoolLiteralGraphNode : ValueContainerGraphNode<bool> {
    private readonly CheckBox literal = new();
    public override bool Value { get => literal.ButtonPressed; protected set => literal.ButtonPressed = value; }
    public BoolLiteralGraphNode() {
        AddChild(literal);
        SetSlotEnabledRight(0, true);
        Resizable = false;
    }
}