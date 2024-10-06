using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public class Event(EventTrigger strart, EventTrigger end, Event.EventCallback update) : ICBExposeable {
    private ICBExposeable boundTo;
    private NativeObject cachedObject;
    public delegate void EventCallback(Object @this);
    public bool active = false;
    public int executionCount = 0;
    public readonly EventTrigger strart = strart;
    public readonly EventTrigger end = end;
    private readonly EventCallback update = update;

    public NativeObject ToObject() {
        return new NativeObject(this);
    }

    public void Bind(ICBExposeable target) {
        boundTo = target;
        cachedObject = boundTo.ToObject();
    }

    public void Update() {
        update(cachedObject);
    }
}