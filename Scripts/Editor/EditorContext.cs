using System.Collections.Generic;
using System.Collections.ObjectModel;
using PCE.Chart;

namespace PCE.Editor;
public static class EditorContext {
    public delegate void OnInitalized();
    public static event OnInitalized Initalized;
    public delegate void OnJudgelineListChanged();
    public static event OnJudgelineListChanged JudgelineListChanged;
    public delegate void OnSelectedJudgelineChanged();
    public static event OnSelectedJudgelineChanged SelectedJudgelineChanged;
    public delegate void OnBPMChangesChanged(Judgeline judgeline);
    public static event OnBPMChangesChanged BPMChangesChanged;
    public delegate void OnEventAdded(Judgeline judgeline);
    public static event OnEventAdded EventAdded;

    public static Chart.Chart Chart { get; private set; }
    private static readonly List<Judgeline> judgelineList = [];
    public static ReadOnlyCollection<Judgeline> Judgelines => judgelineList.AsReadOnly();
    private static Judgeline _selectedJudgeline;
    public static Judgeline SelectedJudgeline {
        get => _selectedJudgeline;
        set {
            _selectedJudgeline = value;
            SelectedJudgelineChanged?.Invoke();
        }
    }

    public static void Initalize(Chart.Chart chart) {
        Chart = chart;
        SetupChart();
        Initalized?.Invoke();
    }

    public static void SetupChart() {
        ChartContext.Reset();
        ChartContext.Initalize(Chart);
        Chart.Reset();
        Chart.SetMusic(Project.SelectedProject.Audio);
        // TODO: transform groups
        foreach (Judgeline judgeline in judgelineList) {
            judgeline.AttachTo(Chart.rootGroup);
        }

        ICBExposeableEditorExtension.InjectEvents();
    }

    public static void AddJudgeline(Judgeline judgeline) {
        judgelineList.Add(judgeline);
        JudgelineListChanged?.Invoke();
    }

    public static void RemoveJudgeline(Judgeline judgeline) {
        judgelineList.Remove(judgeline);
        if (judgeline == SelectedJudgeline)
            SelectedJudgeline = null;

        JudgelineListChanged?.Invoke();
    }

    public static void AddBPMChange(Judgeline judgeline, double time, float bpm) {
        UpdateBPMChangeBPM(judgeline, time, bpm);
    }

    public static void SwapBPMChangeTime(Judgeline judgeline, double currentTime, double newTime) {
        judgeline.bpmChanges[newTime] = judgeline.bpmChanges[currentTime];
        judgeline.bpmChanges.Remove(currentTime);
        BPMChangesChanged?.Invoke(judgeline);
    }

    public static void UpdateBPMChangeBPM(Judgeline judgeline, double time, float newBPM) {
        judgeline.bpmChanges[time] = newBPM;
        BPMChangesChanged?.Invoke(judgeline);
    }

    public static void AddEvent(Judgeline judgeline, EditableEvent @event) {
        ChartContext.AddEvent(judgeline, @event);
        judgeline.GetEvents().Add(@event);
        EventAdded?.Invoke(judgeline);
    }
}