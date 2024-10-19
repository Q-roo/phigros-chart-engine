using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class SelectedJudgeLineInspector : Panel {
    private VBoxContainer bpmChanges;

    public override void _Ready() {
        bpmChanges = GetNode<VBoxContainer>("HSplitContainer/BPMChangeList/VBoxContainer");
        EditorContext.SelectedJudgelineChanged += OnSelectedJudgelineChanged;
    }

    private void OnSelectedJudgelineChanged() {
        RefreshBPMChanges();
    }

    private void RefreshBPMChanges() {
        foreach (Node child in bpmChanges.GetChildren())
            child.QueueFree();

        if (EditorContext.SelectedJudgeline is null)
            return;

        foreach ((double time, float bpm) in EditorContext.SelectedJudgeline.bpmChanges) {
            HBoxContainer container = new();
            NumberInput timeInput = new() {
                Title = "time",
                Editable = time != 0,
                AllowGreater = true,
                MinValue = 0,
                Step = 0,
                Value = time
            };
            NumberInput bpmInput = new() {
                Title = "bpm",
                AllowGreater = true,
                MinValue = double.MinValue,
                Step = 0,
                Value = bpm
            };
            timeInput.SizeFlagsHorizontal |= SizeFlags.Expand;
            bpmInput.SizeFlagsHorizontal |= SizeFlags.Expand;
            container.AddChild(timeInput);
            container.AddChild(bpmInput);
            bpmChanges.AddChild(container);
        }
    }
}
