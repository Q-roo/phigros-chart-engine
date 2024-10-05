using System;

namespace PCE.Chartbuild.Runtime;

public class FunctionalCBObjectProperty(Func<ObjectValue> getter, Action<ObjectValue> setter) : CBObjectPorperty {
    private readonly Func<ObjectValue> getter = getter;
    private readonly Action<ObjectValue> setter = setter;

    public FunctionalCBObjectProperty(Func<ObjectValue> getter)
    : this(getter, _ => throw new InvalidOperationException("cannot set a read-only property")) { }

    public override void SetValue(ObjectValue value) => setter(value);
    public override ObjectValue GetValue() {
        var a = getter();
        Godot.GD.Print( a );
        return a;
    }
}