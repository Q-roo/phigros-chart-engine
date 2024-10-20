using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class JudgeLineEventsInspector : Panel {
    private HFlowContainer container;

    public override void _Ready() {
        container = GetNode<HFlowContainer>("VBoxContainer/Events");
        EditorContext.SelectedJudgelineChanged += RefreshEvents;
        EditorContext.EventAdded += judgeline => {
            if (judgeline == EditorContext.SelectedJudgeline)
                RefreshEvents();
        };
    }

    private void RefreshEvents() {
        foreach (Node child in container.GetChildren())
            child.QueueFree();

        if (EditorContext.SelectedJudgeline is null)
            return;

        foreach (Event @event in EditorContext.SelectedJudgeline.GetEvents()) {
            Texture2D texture = ResourceLoader.Load<Texture2D>("res://icon.svg");
            HBoxContainer eventContainer = new();
            Label name = new() {Text = @event.GetName() };
            TextureButton edit = new() { TextureNormal = texture, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered };
            TextureButton delete = new() { TextureNormal = texture, StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered };

            edit.Pressed += () => OpenEventEditor(@event);

            eventContainer.AddChild(name);
            eventContainer.AddChild(edit);
            eventContainer.AddChild(delete);
            container.AddChild(eventContainer);
        }
    }

    private void OpenEventEditor(Event @event) {
        if (@event is not EditableEvent editable) {
            OS.Alert("event is not editable", "cannot edit event");
            return;
        }
        OpenEventEditor(editable);
    }

    private void OpenEventEditor(EditableEvent @event) {

    }
}
