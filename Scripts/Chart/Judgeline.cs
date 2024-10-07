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
    public readonly Dictionary<double, float> bpmChanges;

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

    public NativeObject ToObject() {
        return new(
            this,
            key => key switch {
                "size" => new F32(Size),
                "position" => new Vec2(Position),
                "rotation" => new F32(RotationDegrees),
                "add_event" => new NativeFunction(args => {
                    if (args.Length == 0)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].Value is not Event @event)
                        throw new ArgumentException("the first argument needs to be an event");

                    ChartContext.AddEvent(this, @event);
                }),
                "add_note" => new NativeFunction(args => {
                    if (args.Length == 0)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].Value is not Note note)
                        throw new ArgumentException("the first argument needs to be a note");

                    note.AttachTo(this);
                }),
                int time => new F32(GetClosestBpm(time)),
                float time => new F32(GetClosestBpm(time)),
                _ => throw new KeyNotFoundException()
            },
            (key, value) => {
                switch (key) {
                    case "size":
                        Size = value.ToF32().value;
                        break;
                    case "position":
                        Position = value.ToVec2().value;
                        break;
                    case "rotation":
                        RotationDegrees = value.ToF32().value;
                        break;
                    case int time:
                        if (ChartContext.Chart.IsInitalized)
                            throw new InvalidOperationException("cannot change bpm values after initalization");

                        bpmChanges[time] = value.ToF32().value;
                        break;
                    case float time:
                        if (ChartContext.Chart.IsInitalized)
                            throw new InvalidOperationException("cannot change bpm values after initalization");

                        bpmChanges[time] = value.ToF32().value;
                        break;
                    default:
                        throw new KeyNotFoundException();
                }
            }
        );
    }

    public float GetClosestBpm(double time) {
        float bpm = bpmChanges[0];

        if (bpmChanges.TryGetValue(time, out bpm))
            return bpm;

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

    public override int GetHashCode() {
        return name.GetHashCode();
    }
    public override string ToString() {
        return $"judgeline({name} ({bpm})";
    }
}