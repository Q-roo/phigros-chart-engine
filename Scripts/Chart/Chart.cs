using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using PCE.Chart.Util;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public static CompatibilityLevel Platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");
    // time is in seconds
    public double CurrentTime { get; private set; }
    public double DeltaTime { get; private set; }
    public bool JustStarted { get; private set; }
    public bool IsInitalized { get; private set; }
    // TODO: current score

    private readonly List<Event> inactiveEvents = [];
    private readonly List<Event> activeEvents = [];
    public readonly HashSet<StringName> signals  = [];
    public readonly List<Judgeline> judgelines = [];

    private readonly AudioStreamPlayer audioPlayer = new();

    public override void _Ready() {
        AddChild(audioPlayer);
        AddChild(rootGroup);
        Reset();
    }

    public void SetMusic(AudioStream stream) {
        audioPlayer.Stream = stream;
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
        audioPlayer.Stream = null;
    }

    public void BeginRender() {
        SetProcess(true);
        // add on start events
        JustStarted = true;
        AddActiveEvents();
        JustStarted = false;
        IsInitalized = true;
        SetNoteYPositions();
        audioPlayer.Play();
    }

    private void SetNoteYPositions() {
        foreach (Judgeline judgeline in judgelines)
            foreach (Note note in judgeline.notes) {
                Vector2 position = note.Position;

                double y = CalculateYPosition(note.time, judgeline);

                position.Y -= (float)y;

                if (note.type == NoteType.Hold) {
                    double heigth = CalculateYPosition(note.time + note.holdTime, judgeline) - y;
                    Vector2 scale = note.Scale;
                    scale.Y = (float)(heigth / note.Texture.GetSize().Y);
                    note.Scale = scale;
                }

                // Y offset
                // (y center is at the center of the sprite but it needs to be on the bottom)
                position.Y -= note.Texture.GetSize().Y * note.Scale.Y / 2;

                note.Position = position;
            }
    }

    private double CalculateYPosition(double time, Judgeline judgeline) {
        double y = 0;

        // times of bpm changes in seconds
        double[] keys = [..judgeline.bpmChanges.Keys];

        Debug.Assert(keys.Length != 0 || keys[0] != 0, "a judgeline should have at least one bpm change at time 0");

        // it's more clear this way
        if (keys.Length == 1)
            return time.ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;

        for (int i = 0; i < keys.Length; i++) {
            double key = keys[i];
            // the last key
            // treat this case similarly to when there's only one key
            if (i == keys.Length - 1)
                return y + (time - key).ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;

            double nextKey = keys[i + 1];
            // the next kext bpm change will occour only after `time`
            // so this can be trated as if this was the last key
            if (nextKey > time)
                return y + (time - key).ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;

            y += (nextKey - key).ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;
        }

        return y;
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
}