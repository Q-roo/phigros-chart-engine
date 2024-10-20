using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class SelectedJudgeLineInspector : Panel {
    private VBoxContainer bpmChanges;

    public override void _Ready() {
        bpmChanges = GetNode<VBoxContainer>("HSplitContainer/VBoxContainer/BPMChangeList/VBoxContainer");
        EditorContext.SelectedJudgelineChanged += RefreshBPMChanges;
        EditorContext.BPMChangesChanged += RefreshBPMChanges;
    }

    private void RefreshBPMChanges() => RefreshBPMChanges(EditorContext.SelectedJudgeline);

    private void RefreshBPMChanges(Judgeline judgeline) {
        foreach (Node child in bpmChanges.GetChildren())
            child.QueueFree();

        if (judgeline is null)
            return;


        foreach ((double initalTime, float initalBpm) in judgeline.bpmChanges) {
            double time = initalTime;
            HBoxContainer container = new() {
                CustomMinimumSize = new(0, 30)
            };
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
                Value = initalBpm
            };
            Button delete = new() { Text = "remove" };
            timeInput.SizeFlagsHorizontal |= SizeFlags.Expand;
            bpmInput.SizeFlagsHorizontal |= SizeFlags.Expand;
            timeInput.spinBox.ValueChanged += newTime => {
                if (judgeline.bpmChanges.ContainsKey(newTime)) {
                    OS.Alert("duplicate BPM");
                    timeInput.spinBox.SetValueNoSignal(time);
                    return;
                }

                EditorContext.SwapBPMChangeTime(judgeline, time, newTime);
                time = newTime;
            };
            bpmInput.spinBox.GetLineEdit().TextSubmitted += _ => {
                EditorContext.UpdateBPMChangeBPM(judgeline, time, (float)bpmInput.Value);
            };
            delete.Pressed += () => judgeline.bpmChanges.Remove(time);
            container.AddChild(timeInput);
            container.AddChild(bpmInput);
            bpmChanges.AddChild(container);
        }
    }
}
