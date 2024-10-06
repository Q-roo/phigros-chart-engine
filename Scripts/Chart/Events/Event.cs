using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public class Event(EventTrigger strart, EventTrigger end, Event.EventCallback update) : ICBExposeable {
    public delegate void EventCallback();
    public bool active = false;
    public int executionCount = 0;
    public readonly EventTrigger strart = strart;
    public readonly EventTrigger end = end;
    public readonly EventCallback update = update;

    public NativeObject ToObject() {
        return new NativeObject(this);
    }
}