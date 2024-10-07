using Godot;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public delegate bool ConditionCallback();
public delegate void OnTriggerTriggered();

public abstract class EventTrigger : ICBExposeable {
    public event OnTriggerTriggered OnTriggerTriggered;
    protected Event @event;
    public abstract bool IsTriggered(Chart chart);
    public void InvokeTrigger() {
        OnTriggerTriggered?.Invoke();
    }

    public NativeObject ToObject() {
        return new(this);
    }

    public void Bind(Event @event) {
        this.@event = @event;
    }
}

public class ManuallyTriggredEvent : EventTrigger {
    // these events aren't triggered by conditions
    // they are triggered manually by the chart
    public override bool IsTriggered(Chart chart) {
        return false;
    }
}

// TODO: implement these triggers

public class OnChartBegin : EventTrigger {
    public override bool IsTriggered(Chart chart) {
        return chart.JustStarted;
    }
}

public class OnChartEnd : ManuallyTriggredEvent; // TODO

public class OnTimeAfter(double time) : EventTrigger {
    private readonly double time = time;
    public override bool IsTriggered(Chart chart) {
        return time >= chart.CurrentTime;
    }
}

public class OnTimeBefore(double time) : EventTrigger {
    private readonly double time = time;
    public override bool IsTriggered(Chart chart) {
        return time <= chart.CurrentTime;
    }
}

public class OnPause : ManuallyTriggredEvent;

public class OnResume : ManuallyTriggredEvent;

public class OnTouchDown(InputEventScreenTouch touch) : ManuallyTriggredEvent {
    private readonly InputEventScreenTouch touch = touch;
}

public class OnTouchMove(InputEventScreenTouch touch) : ManuallyTriggredEvent {
    private readonly InputEventScreenTouch touch = touch;
}

public class OnTouchUp(InputEventScreenTouch touch) : ManuallyTriggredEvent {
    private readonly InputEventScreenTouch touch = touch;
}

public class OnDelayed(EventTrigger delay, EventTrigger trigger) : EventTrigger {
    private readonly EventTrigger delay = delay;
    private readonly EventTrigger trigger = trigger;
    private bool triggerTriggered;

    public override bool IsTriggered(Chart chart) {
        if (triggerTriggered) {
            if (delay.IsTriggered(chart)) {
                triggerTriggered = false; // reset
                return true;
            }
            return false;
        }

        triggerTriggered = trigger.IsTriggered(chart);
        return false;
    }
}

public class OnCondition(ConditionCallback condition) : EventTrigger {
    private readonly ConditionCallback condition = condition;
    public override bool IsTriggered(Chart chart) {
        return condition();
    }
}

public class OnSignal(StringName signal) : EventTrigger {
    private readonly StringName signal = signal;
    public override bool IsTriggered(Chart chart) {
        return chart.signals.Contains(signal);
    }
}

public class OnExecCount(int count) : EventTrigger {
    private readonly int count = count;

    public override bool IsTriggered(Chart chart) {
        return @event.executionCount >= count;
    }
}

// public class OnAttach : ManuallyTriggredEvent;

// public class OnDetach : ManuallyTriggredEvent;

// TODO: OnJudge
// TODO: OnJudgeEnd for hold notes