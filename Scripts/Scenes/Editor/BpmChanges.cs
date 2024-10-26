using System.Linq;
using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class BpmChanges : PanelContainer {
    private readonly Tree list;

    private partial class ItemMetadata : GodotObject {
        public readonly Judgeline judgeline;
        public double PreviousTimeInSeconds { get; private set; }
        public float PreviousBPM { get; private set; }
        private double _currentTimeInSeconds;
        private float _currentBPM;
        public double CurrentTimeInSeconds {
            get => _currentTimeInSeconds;
            set {
                PreviousTimeInSeconds = _currentTimeInSeconds;
                _currentTimeInSeconds = value;
            }
        }
        public float CurrentBPM {
            get => _currentBPM;
            set {
                PreviousBPM = _currentBPM;
                _currentBPM = value;
            }
        }

        public ItemMetadata(Judgeline judgeline, double timeInSeconds, float bpm) {
            this.judgeline = judgeline;
            CurrentTimeInSeconds = timeInSeconds;
            CurrentBPM = bpm;
        }
    }

    public BpmChanges() {
        CustomMinimumSize = new(0, 40);
        list = new() {
            Columns = 3,
            HideRoot = true,
        };
        list.SetColumnExpand(2, false);
        AddChild(list);
        ChartContext.ChildOrderChanged += _ => Refresh();
        list.CustomPopupEdited += (arrowClicked) => {
            if (!arrowClicked)
                return;

            Judgeline judgeline = (Judgeline)list.GetEdited().GetMetadata(0);
            // TODO: get default bpm from scope rules
            (double time, float bpm) = judgeline.bpmChanges.Last();
            judgeline.AddOrModifyBPMChange(time + 10, bpm);

            CallDeferred(MethodName.Refresh); // cannot call refresh while executing this
        };
        list.ButtonClicked += (item, column, id, mouseButton) => {
            ItemMetadata metadata = (ItemMetadata)item.GetMetadata(0);
            if (metadata.CurrentTimeInSeconds == 0) {
                OS.Alert("cannot remove the inital BPM change", "cannot remove BPM change");
                return;
            }

            metadata.judgeline.RemoveBPMChange(metadata.CurrentTimeInSeconds);
            item.Free();
        };
        list.ItemEdited += () => {
            TreeItem item = list.GetEdited();

            if (item.GetMetadata(0).Obj is ItemMetadata metadata)
                switch (list.GetEditedColumn()) {
                    case 0:
                        double timeInSeconds = item.GetRange(0);
                        if (metadata.judgeline.bpmChanges.ContainsKey(timeInSeconds)) {
                            OS.Alert("duplicate time", "cannot modify BPM change");
                            item.SetRange(0, metadata.CurrentTimeInSeconds);
                            return;
                        } else if (metadata.CurrentTimeInSeconds == 0) {
                            OS.Alert("cannot modify the time of the first BPM change", "cannot modify BPM change");
                            item.SetRange(0, 0);
                            return;
                        }
                        metadata.CurrentTimeInSeconds = timeInSeconds;
                        metadata.judgeline.ChangeBPMChangeTime(metadata.PreviousTimeInSeconds, metadata.CurrentTimeInSeconds);
                        break;
                    case 1:
                        float bpm = (float)item.GetRange(1);
                        metadata.CurrentBPM = bpm;
                        metadata.judgeline.AddOrModifyBPMChange(metadata.CurrentTimeInSeconds, metadata.CurrentBPM);
                        break;
                }

        };
    }

    private void Refresh() {
        list.Clear();
        list.CreateItem(); // root
        DisplayJudgelines(EditorContext.Chart.rootGroup);
    }

    private void DisplayJudgelines(TransformGroup parent) {
        foreach (Judgeline judgeline in parent.judgelines) {
            TreeItem branch = list.CreateItem();
            branch.SetText(0, judgeline.Name);
            branch.SetCellMode(2, TreeItem.TreeCellMode.Custom);
            branch.SetCustomAsButton(2, true);
            branch.SetText(2, "add");
            branch.SetEditable(2, true);
            branch.SetMetadata(0, judgeline);

            foreach ((double timeSeconds, float bpm) in judgeline.bpmChanges) {
                TreeItem item = list.CreateItem(branch);
                item.SetCellMode(0, TreeItem.TreeCellMode.Range);
                item.SetCellMode(1, TreeItem.TreeCellMode.Range);
                item.SetEditable(0, timeSeconds != 0);
                item.SetEditable(1, true);
                item.SetRangeConfig(0, 0, Project.SelectedProject.Audio.GetLength(), 0.001);
                item.SetRangeConfig(1, 0.001, float.MaxValue, 0.001);
                item.SetRange(0, timeSeconds);
                item.SetRange(1, bpm);
                item.SetMetadata(0, new ItemMetadata(judgeline, timeSeconds, bpm));
                item.AddButton(2, ResourceLoader.Load<Texture2D>("res://icon.svg"), tooltipText: "Delete");
            }
        }

        foreach (TransformGroup group in parent.subGroups)
            DisplayJudgelines(group);
    }
}
