using Godot;

namespace PCE.Editor;

public partial class EventGraphEditor : GraphEdit
{
    public EventGraphEditor() {
        PopupRequest += ShowPopup;
    }

    private void ShowPopup(Vector2 localMousePosition) {
        // TODO: different menus for different scenarios
        PopupMenu menu = new() {
            Position = DisplayServer.MouseGetPosition(),
            InitialPosition = Window.WindowInitialPosition.Absolute
        };
        menu.AddItem("TODO");
        menu.PopupHide += menu.QueueFree;
        AddChild(menu);
        // GetViewport().AddChild(menu);
        menu.Show();
    }
}
