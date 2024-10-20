using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNext.Collections.Generic;
using Godot;
using PCE.Chart;
using PCE.Chartbuild.Bindings;

namespace PCE.Editor;

// TODO: c# 13
// public implicit extension JudgelineEditorExtension for Judgeline {

// }

public static class ICBExposeableEditorExtension {
    private readonly static ConditionalWeakTable<ICBExposeable, List<EditableEvent>> events = [];

    public static List<EditableEvent> GetEvents(this ICBExposeable exposable) {
        if (!events.TryGetValue(exposable, out List<EditableEvent> evs)) {
            evs = [];
            events.Add(exposable, evs);
        }

        return evs;
    }

    public static bool IsEventNameUnique(this ICBExposeable exposeable, StringName name) {
        return !exposeable.GetEvents().Exists(it => it.GetName() == name);
    }

    public static void InjectEvents() {
        events.ForEach((it) => {
            foreach (Event @event in it.Value)
                ChartContext.AddEvent(it.Key, @event);
        });
    }
}