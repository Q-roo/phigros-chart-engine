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
        menu.AddItem("TODO");
        menu.PopupHide += menu.QueueFree;
        menu.IndexPressed += idx => {
            switch (idx) {
                case 0:
                    GraphNode node = new() { Title = "trigger" }; // TODO: set position to mouse
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
                    node.AddChild(dropdown);
                    AddChild(node);
                    break;
            }
        };
        AddChild(menu);
        // GetViewport().AddChild(menu);
        menu.Show();
    }
}
