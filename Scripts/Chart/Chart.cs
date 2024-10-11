using System;
using System.Collections.Generic;
using Godot;
using PCE.Chart.Util;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public static CompatibilityLevel Platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");
    // time is in seconds
    public double CurrentTime { get; private set; }
    public double DeltaTime { get; private set; }
    public bool JustStarted { get; private set; }
    public bool IsInitalized { get; private set; }

    private readonly List<Event> inactiveEvents = [];
    private readonly List<Event> activeEvents = [];
    public readonly HashSet<StringName> signals  = [];
    public readonly List<Judgeline> judgelines = [];

    public NativeObject ToObject() {
        return new NativeObjectBuilder(this)
        .AddConstantValue("platform", (int)Platform)
        .AddConstantValue("groups", rootGroup.ToObject()) // should not change
        .AddCallable("add_event", args => {
            if (args.Length == 0)
                throw new ArgumentException("insufficient arguments");

            if (args[0].NativeValue is not Event @event)
                throw new ArgumentException("first argument needs to be an event");

            ChartContext.AddEvent(this, @event);
        })
        .Build();
    }

    public override void _Ready() {
        AddChild(rootGroup);
        Reset();
    }

    public void Reset() {
        foreach (Node child in rootGroup.GetChildren()) {
            child.Free(); // the script's execution will start in this frame
        }

        SetProcess(false);
        JustStarted = false;
        IsInitalized = false;
        CurrentTime = 0;
        inactiveEvents.Clear();
        activeEvents.Clear();
        signals.Clear();
        judgelines.Clear();
    }

    public void BeginRender() {
        SetProcess(true);
        // add on start events
        JustStarted = true;
        AddActiveEvents();
        JustStarted = false;
        IsInitalized = true;
        SetNoteYPositions();
    }

    private void SetNoteYPositions() {
        foreach (Judgeline judgeline in judgelines)
            foreach (Note note in judgeline.notes) {
                Vector2 position = note.Position;

                double[] keys = [..judgeline.bpmChanges.Keys];

                for (int i = 0; i < keys.Length; i++) {
                    double key = keys[i];
                    // double range = (i != keys.Count - 1 ? keys[i + 1] : note.time) - key;
                    double range;
                    float bpm = judgeline.bpmChanges[key];

                    // last bpm in the bpm list,
                    // so the note should use it for the rest of the time
                    if (i == keys.Length - 1)
                        range = note.time - key;
                    else {
                        double next = keys[i + 1];
                        // the note will be judged before the next bpm comes
                        // so treat this as if this was the last bpm
                        if (next > note.time) {
                            position.Y -= (float)((note.time - key).ToBeat(bpm) * ChartGlobals.baseNoteSpeed);
                            break;
                        }

                        range = next - key;
                    }

                    position.Y -= (float)(range.ToBeat(bpm) * ChartGlobals.baseNoteSpeed);
                }

                // Y offset
                // (y center is at the center of the sprite but it needs to be on the bottom)
                position.Y -= note.Texture.GetSize().Y / 2;

                note.Position = position;
                GD.Print(position);
            }
    }

    public override void _Process(double delta) {
        CurrentTime += delta;
        DeltaTime = delta;
        AddActiveEvents();
        signals.Clear();
        FlushEvents();
    }

    // add the events that got activated
    private void AddActiveEvents() {
        for (int i = 0; i < inactiveEvents.Count; i++) {
            Event @event = inactiveEvents[i];
            if (@event.strart.IsTriggered(this)) {
                activeEvents.Add(@event);
                inactiveEvents.RemoveAt(i);
                i--;
            }
        }
    }

    // NOTE: flush is the correct term here, right?
    private void FlushEvents() {
        for (int i = 0; i < activeEvents.Count; i++) {
            Event @event = activeEvents[i];
            // event just got added
            // NOTE: should this also let
            // update to run?
            if (!@event.active) {
                @event.active = true;
                @event.strart.InvokeTrigger();
                @event.executionCount = 0;
            }

            if (@event.end.IsTriggered(this)) {
                @event.active = false;
                @event.end.InvokeTrigger();
                activeEvents.RemoveAt(i);
                i--;
                continue; // don't let update run once more
            }

            @event.Update();
            @event.executionCount++;
        }
    }

    public void RegisterEvent(Event @event) {
        inactiveEvents.Add(@event);
    }
}