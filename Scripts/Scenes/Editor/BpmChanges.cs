using System.Linq;
using Godot;
using PCE.Chart;
using PCE.Chart.Util;

namespace PCE.Editor;

public partial class BpmChanges : PanelContainer {
    private GridContainer list;
    private Button add;

    public override void _Ready() {
        add = GetNode<Button>("VBoxContainer/Add");
        list = GetNode<GridContainer>("VBoxContainer/ScrollContainer/GridContainer");
        ChartContext.BPMListChanged += () => CallDeferred(MethodName.Refresh);
        ChartContext.Initalized += Refresh;
        add.Pressed += () => {
            Entry entry = ChartContext.Chart.bpmList.Last();
            ChartContext.Chart.AddOrModifyBPMChange(entry.beats + 1, entry.bpm);
        };
    }

    private void Refresh() {
        // first 3 are column headers
        foreach (Node child in list.GetChildren().Skip(3))
            child.QueueFree();

        foreach (Entry entry in ChartContext.Chart.bpmList) {
            Triple startTimeAsTriple = entry.timeInSeconds.ToTriple(ChartContext.Chart);
            Button delete = new(); // TODO: icon with a trashcan on it
            SpinBox bpm = new();
            TripleInput time = new();

            delete.Text = "remove";
            delete.TooltipText = "remove this bpm from the list";
            delete.Pressed += () => {
                if (entry.beats == 0)
                    OS.Alert("cannot remove the inital BPM change", "cannot remove BPM change");
                else
                    ChartContext.Chart.RemoveBPMChange(entry.beats);
            };

            time.ExpandToTextLength = true;
            time.SetValueNoSignal(startTimeAsTriple);
            time.ValueChanged += () => {
                double newBeat = time.Value.ToBeat();
                if (entry.beats == newBeat)
                    return;
                else if (entry.beats == 0) {
                    OS.Alert("cannot modify the time of the first BPM change", "cannot modify BPM change");
                    time.SetValueNoSignal(startTimeAsTriple);
                    return;
                } else if (ChartContext.Chart.bpmList.HasTime(newBeat)) {
                    OS.Alert("duplicate time", "cannot modify BPM change");
                    time.SetValueNoSignal(startTimeAsTriple);
                    return;
                }

                ChartContext.Chart.ChangeBPMChangeTime(entry.beats, time.Value.ToBeat());
            };

            bpm.MinValue = 0.0001;
            bpm.Step = 0;
            bpm.CustomArrowStep = 1;
            bpm.AllowGreater = true;
            bpm.CustomMinimumSize = new(200, 0);
            bpm.Alignment = HorizontalAlignment.Center;
            bpm.SetValueNoSignal(entry.bpm);
            bpm.ValueChanged += value => ChartContext.Chart.AddOrModifyBPMChange(entry.beats, (float)value);

            list.AddChild(delete);
            list.AddChild(bpm);
            list.AddChild(time);
        }
    }
}
