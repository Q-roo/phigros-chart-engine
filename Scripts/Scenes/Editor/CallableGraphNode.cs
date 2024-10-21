using Godot;

namespace PCE.Editor;

public partial class CallableGraphNode : GraphNode {
    private readonly Button add = new();
    public CallableGraphNode() {
        Title = "callable";
        add.Text = "add argument";
        AddChild(new Label() { Text = "this", HorizontalAlignment = HorizontalAlignment.Right });
        AddChild(new Label() { Text = "execute", HorizontalAlignment = HorizontalAlignment.Right });
        // spacer
        AddChild(new Control() { CustomMinimumSize = new(0, 30)});
        AddChild(add);
        add.Pressed += OnAddPressed;
        SetSlotEnabledRight(0, true);
        SetSlotEnabledRight(1, true);
        Resizable = true;
    }

    private void OnAddPressed() {
        HBoxContainer argument = new();
        LineEdit name = new() {
            PlaceholderText = "name"
        };
        name.SizeFlagsHorizontal |= SizeFlags.Expand;
        Button delete = new() {
            Text = "delete"
        };
        delete.Pressed += () => {
            // calling only queue free
            // is a bit buggy
            // when it comes to resizing
            // this was buggy with argument.GetIndex()
            // it seems like set slot is not updated immediately
            // so get index becomes invalid by then
            int idx = add.GetIndex() - 1;
            SetSlotEnabledLeft(idx, false);
            SetSlotEnabledRight(idx, false);
            // causes index out of bounds internally for some reason
            // RemoveChild(argument);
            argument.Hide();
            argument.QueueFree();
            Vector2 size = Size;
            size.Y -= argument.Size.Y;
            Size = size;
        };
        argument.AddChild(name);
        argument.AddChild(delete);
        AddChild(argument);
        MoveChild(argument, add.GetIndex());
        SetSlotEnabledLeft(argument.GetIndex(), true);
        SetSlotEnabledRight(argument.GetIndex(), true);
    }
}