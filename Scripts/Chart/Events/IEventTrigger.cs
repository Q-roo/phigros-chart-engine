using Godot;

namespace PCE.Chart;

public delegate bool ConditionCallback();

public interface IEventTrigger;

// TODO: implement these triggers

public struct OnChartBegin : IEventTrigger;
public struct OnChartEnd : IEventTrigger;
public struct OnTime(float time) : IEventTrigger;
public struct OnPause : IEventTrigger;
public struct OnResume : IEventTrigger;
public struct OnTouchDown(InputEventScreenTouch touch) : IEventTrigger;
public struct OnTouchMove(InputEventScreenTouch touch) : IEventTrigger;
public struct OnTouchUp(InputEventScreenTouch touch) : IEventTrigger;
public struct OnDelayed(IEventTrigger delay) : IEventTrigger;
public struct OnCondition(ConditionCallback condition) : IEventTrigger;
public struct OnSignal(Signal signal) : IEventTrigger;
public struct OnAttach : IEventTrigger;
public struct OnDetach : IEventTrigger;
// TODO: OnJudge
// TODO: OnJudgeEnd for hold notes