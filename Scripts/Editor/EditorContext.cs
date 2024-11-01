using System.Collections.Generic;
using PCE.Chart;

namespace PCE.Editor;
public static class EditorContext {
    public delegate void OnInitalized();
    public static event OnInitalized Initalized;

    public static Chart.Chart Chart { get; private set; }

    public static void Initalize(Chart.Chart chart) {
        Chart = chart;
        SetupChart();
        Initalized?.Invoke();
    }

    public static void SetupChart() {
        ChartContext.Reset();
        ChartContext.Initalize(Chart);
        Chart.Reset();
        ICBExposeableEditorExtension.InjectEvents();
    }
}