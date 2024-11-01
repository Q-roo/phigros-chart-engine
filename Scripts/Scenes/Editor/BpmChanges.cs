using System.Linq;
using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class BpmChanges : PanelContainer {
    private readonly Tree list;

    private partial class ItemMetadata : GodotObject {
        public readonly Entry entry;
        public double PreviousTimeInBeats { get; private set; }
        public float PreviousBPM { get; private set; }
        private double _currentTimeInBeats;
        private float _currentBPM;
        public double CurrentTimeInBeats {
            get => _currentTimeInBeats;
            set {
                PreviousTimeInBeats = _currentTimeInBeats;
                _currentTimeInBeats = value;
            }
        }
        public float CurrentBPM {
            get => _currentBPM;
            set {
                PreviousBPM = _currentBPM;
                _currentBPM = value;
            }
        }

        public ItemMetadata(Entry entry) {
            this.entry = entry;
            CurrentTimeInBeats = entry.beats;
            CurrentBPM = entry.bpm;
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
        list.CustomPopupEdited += (arrowClicked) => {
            if (!arrowClicked)
                return;

            // TODO: get default bpm from scope rules
            Entry last = ChartContext.Chart.bpmList.Last();
            ChartContext.Chart.AddOrModifyBPMChange(last.beats + 1, last.bpm);

            CallDeferred(MethodName.Refresh); // cannot call refresh while executing this
        };
        list.ButtonClicked += (item, column, id, mouseButton) => {
            ItemMetadata metadata = (ItemMetadata)item.GetMetadata(0);
            if (metadata.CurrentTimeInBeats == 0) {
                OS.Alert("cannot remove the inital BPM change", "cannot remove BPM change");
                return;
            }

            ChartContext.Chart.RemoveBPMChange(metadata.CurrentTimeInBeats);
            item.Free();
        };
        list.ItemEdited += () => {
            TreeItem item = list.GetEdited();

            if (item.GetMetadata(0).Obj is ItemMetadata metadata)
                switch (list.GetEditedColumn()) {
                    case 0:
                        double timeInBeats = item.GetRange(0);
                        if (ChartContext.Chart.bpmList.HasTime(timeInBeats)) {
                            OS.Alert("duplicate time", "cannot modify BPM change");
                            item.SetRange(0, metadata.CurrentTimeInBeats);
                            return;
                        } else if (metadata.CurrentTimeInBeats == 0) {
                            OS.Alert("cannot modify the time of the first BPM change", "cannot modify BPM change");
                            item.SetRange(0, 0);
                            return;
                        }
                        metadata.CurrentTimeInBeats = timeInBeats;
                        ChartContext.Chart.ChangeBPMChangeTime(metadata.PreviousTimeInBeats, metadata.CurrentTimeInBeats);
                        break;
                    case 1:
                        float bpm = (float)item.GetRange(1);
                        metadata.CurrentBPM = bpm;
                        ChartContext.Chart.AddOrModifyBPMChange(metadata.CurrentTimeInBeats, metadata.CurrentBPM);
                        break;
                }

        };
    }

    private void Refresh() {
        list.Clear();
        list.CreateItem(); // root
        foreach (Entry entry in ChartContext.Chart.bpmList) {
            TreeItem item = list.CreateItem();
                item.SetCellMode(0, TreeItem.TreeCellMode.Range);
                item.SetCellMode(1, TreeItem.TreeCellMode.Range);
                item.SetEditable(0, entry.beats != 0);
                item.SetEditable(1, true);
                item.SetRangeConfig(0, 0, Project.SelectedProject.Audio.GetLength(), 0.001);
                item.SetRangeConfig(1, 0.001, float.MaxValue, 0.001);
                item.SetRange(0, entry.beats);
                item.SetRange(1, entry.bpm);
                item.SetMetadata(0, new ItemMetadata(entry));
                item.AddButton(2, ResourceLoader.Load<Texture2D>("res://icon.svg"), tooltipText: "Delete");
        }
    }
}
