using System.Runtime.CompilerServices;
using Godot;
using PCE.Chart;

namespace PCE.Editor;

public static class EventEditorExtension {
    private static readonly ConditionalWeakTable<Event, StringName> eventNames = [];
    public static StringName GetName(this Event @event) {
        if (!eventNames.TryGetValue(@event, out StringName name)) {
            name = @event.GenerateName();
            eventNames.Add(@event, name);
        }
        return name;

    }

    public static void SetName(this Event @event, StringName name) {
        eventNames.AddOrUpdate(@event, name);
    }

    private static StringName GenerateName(this Event @event) {
        if (@event.BoundTo is null)
        return "event";
        return $"event{@event.BoundTo.GetEvents().Count}";
    }
}