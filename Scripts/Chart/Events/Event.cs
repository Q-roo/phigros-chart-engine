using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;
using PCE.Editor;

namespace PCE.Chart;

public class Event : ICBExposeable {
    public ICBExposeable BoundTo { get; private set; }
    private NativeObject cachedObject;
    public delegate void EventCallback(Object @this);
    public bool active;
    public int executionCount;
    public EventTrigger Strart {get; protected set;}
    public EventTrigger End {get; protected set;}
    protected EventCallback update;

    public Event(EventTrigger strart, EventTrigger end, EventCallback update) {
        Strart = strart;
        End = end;
        this.update = update;
        active = false;
        executionCount = 0;
        strart?.Bind(this);
        end?.Bind(this);
    }

    public NativeObject ToObject() {
        return new NativeObject(this);
    }

    public void Bind(ICBExposeable target) {
        BoundTo = target;
        cachedObject = BoundTo.ToObject();
    }

    public void Update() {
        update(cachedObject);
    }

    public EditableEvent ToEditable() => new() {
        BoundTo = BoundTo,
        cachedObject = cachedObject,
        Strart = Strart,
        End = End,
        update = update
    };
}