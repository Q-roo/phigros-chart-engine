using System;
namespace PCE.Chartbuild.Runtime;

public class FunctionalObjectValue : ObjectValue {
    private readonly Func<object, CBObject> getter;
    private readonly Action<object, CBObject> setter;

    public FunctionalObjectValue(object value, Func<object, CBObject> getter, Action<object, CBObject> setter)
    : base(value) {
        Type = ValueType.Property;
        this.getter = getter;
        this.setter = setter;
    }

    public FunctionalObjectValue(object value, Func<object, CBObject> getter)
    : this(value, getter, (_, _) => throw new InvalidOperationException("cannot set a read-only property")) { }

    public override CBObject GetMember(object key) => getter(key);
    public override void SetMember(object key, CBObject value) => setter(key, value);
}