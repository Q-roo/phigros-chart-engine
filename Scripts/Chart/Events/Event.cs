namespace PCE.Chart;

public class Event {
    public delegate void EventCallback();
    public readonly IEventTrigger strart;
    public readonly IEventTrigger end;
    public readonly EventCallback begin;
    public readonly EventCallback finish;
    public readonly EventCallback update;
}