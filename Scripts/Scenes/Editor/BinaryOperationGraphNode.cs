using Godot;

namespace PCE.Editor;

public partial class BinaryOperationGraphNode : GraphNode {
    private readonly OptionButton @operator = new();

    public BinaryOperationGraphNode() {
        Title = "binary operation";
        AddChild(new Label() { Text = "a" });
        AddChild(new Label() { Text = "operator" });
        AddChild(@operator);
        AddChild(new Label() { Text = "b" });

        SetSlotEnabledLeft(0, true);
        SetSlotEnabledRight(2, true);
        SetSlotEnabledLeft(3, true);
        @operator.AddItem("+");
        @operator.AddItem("-");
        @operator.AddItem("*");
        @operator.AddItem("/");
        @operator.AddItem("%");
        @operator.AddItem("**");
        @operator.AddSeparator();
        @operator.AddItem("<<");
        @operator.AddItem(">>");
        @operator.AddItem("|");
        @operator.AddItem("^");
        @operator.AddItem("&");
        @operator.AddSeparator();
        @operator.AddItem("==");
        @operator.AddItem("!=");
        @operator.AddItem("<");
        @operator.AddItem("<=");
        @operator.AddItem(">");
        @operator.AddItem(">=");
    }
}