using Godot;

namespace PCE.Editor;

public partial class EventGraphEditor : GraphEdit {
    public EventGraphEditor() {
        PopupRequest += ShowPopup;
    }

    private void ShowPopup(Vector2 localMousePosition) {
        // TODO: different menus for different scenarios
        PopupMenu menu = new() {
            Position = DisplayServer.MouseGetPosition(),
            InitialPosition = Window.WindowInitialPosition.Absolute
        };
        menu.AddItem("Add trigger node");
        menu.AddItem("Add string literal node");
        menu.AddItem("Add i32 literal node");
        menu.AddItem("Add f32 literal node");
        menu.AddItem("TODO");
        menu.PopupHide += menu.QueueFree;
        menu.IndexPressed += idx => {
            switch (idx) {
                case 0:
                    GraphNode node = new() { Title = "trigger" }; // TODO: set position to mouse
                    Label slot0 = new();
                    Label slot1 = new();
                    node.AddChild(new Label() { Text = "type" });
                    OptionButton dropdown = new();
                    dropdown.AddItem("begin");
                    dropdown.AddItem("end");
                    dropdown.AddItem("pause");
                    dropdown.AddItem("resume");
                    dropdown.AddItem("before");
                    dropdown.AddItem("after");
                    dropdown.AddItem("exec");
                    dropdown.AddItem("once");
                    dropdown.AddItem("signal");
                    dropdown.AddItem("delay");
                    dropdown.AddItem("condtion");
                    dropdown.ItemSelected += idx => {
                        slot0.Text = string.Empty;
                        slot1.Text = string.Empty;

                        switch (idx) {
                            case 4 or 5:
                                slot0.Text = "time";
                                break;
                            case 6:
                                slot0.Text = "count";
                                break;
                            case 8:
                                slot0.Text = "name";
                                break;
                            case 9:
                                slot0.Text = "delay";
                                slot1.Text = "trigger";
                                break;
                            case 10:
                                slot0.Text = "predicate";
                                break;
                        }

                        node.SetSlotEnabledLeft(2, slot0.Text != string.Empty);
                        node.SetSlotEnabledLeft(3, slot1.Text != string.Empty);
                    };
                    node.SetSlotEnabledRight(0, true);
                    node.AddChild(dropdown);
                    node.AddChild(slot0);
                    node.AddChild(slot1);
                    AddChild(node);
                    break;
                case 1:
                    AddChild(new StringLiteralGraphNode());
                    break;
                case 2:
                    AddChild(new I32LiteralGraphNode());
                    break;
                case 3:
                    AddChild(new F32LiteralGraphNode());
                    break;
            }
        };
        AddChild(menu);
        // GetViewport().AddChild(menu);
        menu.Show();
    }
}
