using Godot;

namespace PCE.Editor;

public partial class NewBpmChangeInput : HBoxContainer
{
    private NumberInput bpm;
    private NumberInput time;
    private Button add;

    public override void _Ready() {
        bpm = GetNode<NumberInput>("Bpm");
        time = GetNode<NumberInput>("Time");
        add = GetNode<Button>("Add");

        add.Pressed += OnAddPressed;
    }

    private void OnAddPressed() {
        if (EditorContext.SelectedJudgeline is null) {
            OS.Alert("no judgeline selected", "cannot add BPM change");
            return;
        }
        if (EditorContext.SelectedJudgeline.bpmChanges.ContainsKey(time.Value)) {
            OS.Alert("duplicate time", "cannot add BPM change");
            return;
        }

        EditorContext.AddBPMChange(EditorContext.SelectedJudgeline, time.Value, (float)bpm.Value);
    }
}
