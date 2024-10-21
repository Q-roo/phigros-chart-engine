using Godot;

namespace PCE.Editor;

public partial class StringLiteralGraphNode : ValueContainerGraphNode<string> {
    private readonly TextEdit literal = new();
    public override string Value { get => literal.Text; protected set => literal.Text = value; }

    public StringLiteralGraphNode() {
        literal.SizeFlagsVertical |= SizeFlags.Expand;
        AddChild(literal);
        SetSlotEnabledRight(0, true);
        Size = new(180, 100);
    }
}