using Godot;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public static class ChartContext {
    public delegate void EventHandlerSimple();
    public delegate void EventHandlerJudgeline(Judgeline judgeline);
    public delegate void EventHandlerNote(Note note);
    public delegate void EventHandlerTransformGroup(TransformGroup transformGroup);
    public delegate void OnJudgelineAttachmentChanged(Judgeline judgeline, TransformGroup parent);
    public delegate void OnTransformGroupAttachmentChanged(TransformGroup transformGroup, TransformGroup parent);

    public static event EventHandlerSimple Initalized;
    public static event EventHandlerSimple Reseted;
    public static event OnJudgelineAttachmentChanged JudgelineAttached;
    public static event OnJudgelineAttachmentChanged JudgelineDetached;
    public static event OnTransformGroupAttachmentChanged TransformGroupAttached;
    public static event OnTransformGroupAttachmentChanged TransformGroupDetached;
    public static event EventHandlerTransformGroup ChildOrderChanged;
    public static event EventHandlerJudgeline BPMListChanged;
    public static event EventHandlerSimple FocusedJudgelineChanged;
    public static event EventHandlerSimple FocusedNoteChanged;

    public static Chart Chart { get; private set; }
    public static int JudgelineCount { get; private set; }
    public static int NoteCount { get; private set; }
    public static Judgeline FocusedJudgeline {get; private set;}
    public static Note FocusedNote {get; private set;}

    public static void Reset() {
        Chart = null;
        JudgelineCount = 0;
        NoteCount = 0;
        Reseted?.Invoke();
    }

    public static void Initalize(Chart chart) {
        Chart = chart;
        Initalized?.Invoke();
    }

    public static void Focus(this Judgeline judgeline) {
        FocusedJudgeline = judgeline;
        FocusedJudgelineChanged?.Invoke();
    }

    public static void Focus(this Note note) {
        FocusedNote = note;
        FocusedNoteChanged?.Invoke();
    }

    public static void ChangeBPMChangeTime(this Judgeline judgeline, double currentTime, double newTime) {
        judgeline.bpmChanges[newTime] = judgeline.bpmChanges[currentTime];
        judgeline.bpmChanges.Remove(currentTime);
        BPMListChanged?.Invoke(judgeline);
    }

    public static void AddOrModifyBPMChange(this Judgeline judgeline, double time, float bpm) {
        judgeline.bpmChanges[time] = bpm;
        BPMListChanged?.Invoke(judgeline);
    }

    public static void RemoveBPMChange(this Judgeline judgeline, double time) {
        judgeline.bpmChanges.Remove(time);
        BPMListChanged?.Invoke(judgeline);
    }

    public static void AttachTo(this Judgeline judgeline, TransformGroup group) {
        group.AddChild(judgeline);
        group.judgelines.Add(judgeline);
        group.childOrder.Add(judgeline);
        judgeline.parentGroup = group;
        Chart.judgelines.Add(judgeline);
        JudgelineAttached?.Invoke(judgeline, group);
        ChildOrderChanged?.Invoke(group);
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
        TransformGroupAttached?.Invoke(group, parentGroup);
        ChildOrderChanged?.Invoke(parentGroup);
    }

    public static void Detach(this Judgeline judgeline) {
        TransformGroup group = judgeline.parentGroup;
        group.RemoveChild(judgeline);
        group.judgelines.Remove(judgeline);
        group.childOrder.Remove(judgeline);
        judgeline.parentGroup = null;
        Chart.judgelines.Remove(judgeline);
        JudgelineDetached?.Invoke(judgeline, group);
        ChildOrderChanged(group);
    }

    public static void Detach(this Note note) {
        note.Parent.RemoveChild(note);
        note.Parent.notes.Remove(note);
        note.Parent = null;
    }

    public static void Detach(this TransformGroup group) {
        TransformGroup parentGroup = group.parentGroup;
        parentGroup.subGroups.Remove(group);
        parentGroup.childOrder.Remove(group);
        parentGroup.RemoveChild(group);
        group.parentGroup = null;
        TransformGroupDetached?.Invoke(group, parentGroup);
        ChildOrderChanged.Invoke(parentGroup);
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
        ChildOrderChanged?.Invoke(parent);
    }

    public static StringName GetJudgelineName() => $"jl#{JudgelineCount++}"; // ensure that each name is unique
    private static StringName GetNoteName() => $"n${NoteCount++}";

    public static void AddEvent(ICBExposeable bindTo, Event @event) {
        @event.Bind(bindTo);
        Chart.RegisterEvent(@event);
    }
}