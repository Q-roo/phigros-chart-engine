using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public class Event : ICBExposeable {
    private ICBExposeable boundTo;
    private NativeObject cachedObject;
    public delegate void EventCallback(Object @this);
    public bool active;
    public int executionCount;
    public readonly EventTrigger strart;
    public readonly EventTrigger end;
    private readonly EventCallback update;

    public Event(EventTrigger strart, EventTrigger end, EventCallback update) {
        this.strart = strart;
        this.end = end;
        this.update = update;
        active = false;
        executionCount = 0;
        strart.Bind(this);
        end.Bind(this);
    }

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