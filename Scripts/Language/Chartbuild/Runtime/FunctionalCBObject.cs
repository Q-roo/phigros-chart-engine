using System;

namespace PCE.Chartbuild.Runtime;

public class FunctionalCBObject(FunctionalObjectValue value) : CBObject(value) {
    private readonly FunctionalObjectValue value = value;
    public override ObjectValue GetValue() =>value;

    public override void SetValue(ObjectValue value) {
        throw new InvalidOperationException("cannot set a read-only value");
    }
}