using Godot;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public static class ChartContext {
    public static Chart Chart {get; private set;}
    public static int JudgelineCount {get; private set;}

    public static void Reset() {
        Chart = null;
        JudgelineCount = 0;
    }

    public static void Init(Chart chart) {
        Chart = chart;
    }

    public static void AttachTo(this Judgeline judgeline, TransformGroup group) {
        group.AddChild(judgeline);
        group.judgelines.Add(judgeline);
        judgeline.parent = group;
    }

    public static void Detach(this Judgeline judgeline) {
        judgeline.parent.RemoveChild(judgeline);
        judgeline.parent.judgelines.Remove(judgeline);
        judgeline.parent = null;
    }

    public static StringName GetJudgelineName() => $"jl#{JudgelineCount++}"; // ensure that each name is unique

    public static void AddEvent(ICBExposeable bindTo, Event @event) {
        @event.Bind(bindTo);
        Chart.RegisterEvent(@event);
    }
}