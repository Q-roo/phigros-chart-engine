using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public static class ChartContext {
    public static Chart Chart { get; private set; }
    public static int JudgelineCount { get; private set; }
    public static int NoteCount { get; private set; }

    public static void Reset() {
        Chart = null;
        JudgelineCount = 0;
        NoteCount = 0;
    }

    public static void Initalize(Chart chart) {
        Chart = chart;
    }

    public static void AttachTo(this Judgeline judgeline, TransformGroup group) {
        group.AddChild(judgeline);
        group.judgelines.Add(judgeline);
        group.childOrder.Add(judgeline);
        judgeline.parentGroup = group;
        Chart.judgelines.Add(judgeline);
    }

    public static void AttachTo(this Note note, Judgeline judgeline) {
        judgeline.AddChild(note);
        judgeline.notes.Add(note);
        note.Parent = judgeline;
        note.Name = GetNoteName();
    }

    public static void AttachTo(this TransformGroup group, TransformGroup parentGroup) {
        parentGroup.AddChild(group);
        parentGroup.subGroups.Add(group);
        parentGroup.childOrder.Add(group);
        group.parentGroup = parentGroup;
    }

    public static void Detach(this Judgeline judgeline) {
        judgeline.parentGroup.RemoveChild(judgeline);
        judgeline.parentGroup.judgelines.Remove(judgeline);
        judgeline.parentGroup.childOrder.Remove(judgeline);
        judgeline.parentGroup = null;
        Chart.judgelines.Remove(judgeline);
    }

    public static void Detach(this TransformGroup group) {
        group.parentGroup.subGroups.Remove(group);
        group.parentGroup.childOrder.Remove(group);
        group.parentGroup.RemoveChild(group);
        group.parentGroup = null;
    }

    public static void MoveTo(this Judgeline judgeline, int index) {
        judgeline.MoveTo(judgeline.parentGroup, index);
    }

    public static void MoveTo(this TransformGroup group, int index) {
        group.MoveTo(group.parentGroup, index);
    }

    // NOTE: currently, hash sets are used
    private static void MoveTo<T>(this T node, TransformGroup parent, int index) where T : Node2D {
        parent.MoveChild(node, index);
        parent.childOrder.Remove(node);
        parent.childOrder.Insert(index, node);
    }

    public static StringName GetJudgelineName() => $"jl#{JudgelineCount++}"; // ensure that each name is unique
    private static StringName GetNoteName() => $"n${NoteCount++}";

    public static void AddEvent(ICBExposeable bindTo, Event @event) {
        @event.Bind(bindTo);
        Chart.RegisterEvent(@event);
    }
}