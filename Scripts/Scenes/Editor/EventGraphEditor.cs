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
        menu.AddItem("Add bool literal node");
        menu.AddItem("Add vec2 node");
        menu.AddItem("Add callable node");
        menu.AddItem("Add binary operation node");
        menu.AddItem("TODO");
        menu.PopupHide += menu.QueueFree;
        menu.IndexPressed += idx => {
            switch (idx) {
                case 0:
                    AddChild(new EventTriggerGraphNode());
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
                case 4:
                    AddChild(new BoolLiteralGraphNode());
                    break;
                case 5:
                    AddChild(new Vec2GraphNode());
                    break;
                case 6:
                    AddChild(new CallableGraphNode());
                    break;
                case 7:
                    AddChild(new BinaryOperationGraphNode());
                    break;
            }
        };
        AddChild(menu);
        // GetViewport().AddChild(menu);
        menu.Show();
    }
}
