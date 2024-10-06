using System;
using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public static CompatibilityLevel Platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");
    // time is in seconds
    public double CurrentTime { get; private set; }
    public double DeltaTime {get; private set; }
    public bool JustStarted { get; private set; }

    private readonly List<Event> inactiveEvents = [];
    private readonly List<Event> activeEvents = [];
    public readonly HashSet<StringName> signals  = [];

    public NativeObject ToObject() {
        return new(
            this,
            key => key switch {
                "platform" => new I32((int)Platform),
                "groups" => rootGroup.ToObject(),
                "add_event" => new NativeFunction(args => {
                    if (args.Length == 0)
                        throw new ArgumentException("insufficient arguments");

                    if (args[0].Value is not Event @event)
                        throw new ArgumentException("first argument needs to be an event");

                    inactiveEvents.Add(@event);
                }),
                _ => throw new KeyNotFoundException()
            },
            (Key, value) => {

            }
        );
    }

    public override void _Ready() {
        AddChild(rootGroup);
        Reset();
    }

    public void Reset() {
        foreach (Node child in rootGroup.GetChildren()) {
            child.QueueFree();
        }

        SetProcess(false);
        JustStarted = false;
        CurrentTime = 0;
        inactiveEvents.Clear();
        activeEvents.Clear();
        signals.Clear();
    }

    public void BeginRender() {
        SetProcess(true);
        // add on start events
        JustStarted = true;
        AddEvents();
        JustStarted = false;
    }

    public override void _Process(double delta) {
        CurrentTime += delta;
        DeltaTime = delta;
        AddEvents();
        signals.Clear();
        FlushEvents();
    }

    // add the events that got activated
    private void AddEvents() {
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

            @event.update();
            @event.executionCount++;
        }
    }
}