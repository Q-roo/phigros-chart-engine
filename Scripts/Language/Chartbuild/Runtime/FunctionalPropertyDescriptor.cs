using System;

namespace PCE.Chartbuild.Runtime;

public class FunctionalObjectPropertyDescriptor(Func<ObjectValue> getter, Action<ObjectValue> setter) : CBObjectPropertyDescriptor {
    private readonly Func<ObjectValue> getter = getter;
    private readonly Action<ObjectValue> setter = setter;

    public FunctionalObjectPropertyDescriptor(Func<ObjectValue> getter)
    : this(getter, _ => throw new InvalidOperationException("cannot set a read-only property")) { }

    public override void SetValue(ObjectValue value) => setter(value);
    public override ObjectValue GetValue() => getter();
}