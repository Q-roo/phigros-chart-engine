using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class NewEventInput : HBoxContainer
{
    private TextInput name;
    private Button add;

    public override void _Ready() {
        name = GetNode<TextInput>("Name");
        add = GetNode<Button>("Add");

        add.Pressed += OnAddPressed;
    }

    private void OnAddPressed() {
        if (EditorContext.SelectedJudgeline is null)
        {
            OS.Alert("no judgeline selected", "cannot add new event");
            return;
        }

        if (!EditorContext.SelectedJudgeline.IsEventNameUnique(name.Value)) {
            OS.Alert("name is not unique", "cannot add new event");
            return;
        }

        EditableEvent @event = new();
        @event.SetName(name.Value);

        EditorContext.AddEvent(EditorContext.SelectedJudgeline, @event);
    }
}
