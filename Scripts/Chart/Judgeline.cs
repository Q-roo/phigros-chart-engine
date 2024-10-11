using System;
using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Judgeline : Line2D, ICBExposeable {
    public TransformGroup parent;
    private float _size;
    public float Size {
        get => _size;
        set {
            _size = value;
            Points = [
                new (-value / 2f, 0),
                new (value / 2f, 0)
            ];
        }
    }
    public readonly StringName name;
    public float bpm;
    public readonly List<Note> notes;
    // time in seconds, bpm
    // this should become read-only once the chart starts
    // to be able to calculate note y positions and hold heights
    // keys are in ascending order
    public readonly SortedDictionary<double, float> bpmChanges;

    public Note this[int index] {
        get => notes[index];
        set => notes[index] = value;
    }

    public Judgeline(StringName name, float bpm, float size) {
        Size = size;
        Width = 5;
        this.bpm = bpm;
        this.name = name;
        Name = name;
        Antialiased = true;
        notes = [];
        bpmChanges = new() {
            { 0, bpm }
        };
    }

    public Judgeline()
    : this(ChartContext.GetJudgelineName(), 120, 4000) { }

    public override void _Draw() {
        // the center of the judgeline
        // this looks worse with antialias
        DrawCircle(Vector2.Zero, 5, new(0.12f, 0.56f, 0.78f));
    }

    public NativeObject ToObject() {
        return new NativeObjectBuilder(this)
        .AddGetSetProperty("size", () => Size, value => Size = value)
        .AddGetSetProperty("position", () => Position, value => Position = value)
        .AddGetSetProperty("rotation", () => RotationDegrees, value => RotationDegrees = value)
        .AddCallable("add_event", args => {
            if (args.Length == 0)
                throw new ArgumentException("insufficient arguments");

            if (args[0].NativeValue is not Event @event)
                throw new ArgumentException("the first argument needs to be an event");

            ChartContext.AddEvent(this, @event);
        })
        .AddCallable("add_note", args => {
            if (args.Length == 0)
                throw new ArgumentException("insufficient arguments");

            if (args[0].NativeValue is not Note note)
                throw new ArgumentException("the first argument needs to be a note");

            note.AttachTo(this);
        })
        .SetFallbackGetter(@this => key => {
            double time = key switch {
                int i => i,
                float f => f,
                _ => throw new ArgumentException($"cannot turn {key} into a double")
            };
            return new SetGetProperty(@this, time, (_, _) => GetClosestBpm(time), (_, _, value) => bpmChanges[time] = value);
        })
        .Build();
    }

    public float GetClosestBpm(double time) {
        if (bpmChanges.TryGetValue(time, out float bpm))
            return bpm;

        bpm = bpmChanges[0];

        double[] keys = [.. bpmChanges.Keys];

        for (int i = 0; i < keys.Length - 1; i++) {
            double current = keys[i];
            double next = keys[i + 1];

            if (next > time)
                return bpmChanges[current];

            bpm = bpmChanges[next];
        }

        // there should always be a bpm at 0
        return bpm;
    }

    public float GetCurrentBpm() => GetClosestBpm(ChartContext.Chart.CurrentTime);

    public override int GetHashCode() {
        return name.GetHashCode();
    }
    public override string ToString() {
        return $"judgeline({name} ({bpm})";
    }
}