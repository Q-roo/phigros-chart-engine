using System.Collections.Generic;
using System.Collections.ObjectModel;
using PCE.Chart;

namespace PCE.Editor;
public static class EditorContext {
    public delegate void OnJudgelineListChanged();
    public static event OnJudgelineListChanged JudgelineListChanged;
    public delegate void OnSelectedJudgelineChanged();
    public static event OnSelectedJudgelineChanged SelectedJudgelineChanged;

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
}