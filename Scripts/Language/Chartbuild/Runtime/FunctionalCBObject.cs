using System;

namespace PCE.Chartbuild.Runtime;

public class FunctionalCBObject(Func<ObjectValue, ObjectValue> getter, Action<ObjectValue, ObjectValue> setter) : CBObject {
    private readonly Func<ObjectValue, ObjectValue> getter = getter;
    private readonly Action<ObjectValue, ObjectValue> setter = setter;

    public FunctionalCBObject(Func<ObjectValue, ObjectValue> getter)
    : this(getter, (_, _) => throw new InvalidOperationException("cannot set a read-only property")) { }

    public override ObjectValue GetValue() {
        throw new NotImplementedException();
    }

    public override void SetValue(ObjectValue value) {
        throw new NotImplementedException();
    }
}