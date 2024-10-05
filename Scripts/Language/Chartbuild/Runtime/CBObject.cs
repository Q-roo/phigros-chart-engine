using System;

namespace PCE.Chartbuild.Runtime;

public class CBObject {
    public delegate CBObject CBFunction(params CBObject[] args);
    public delegate void CBFunctionImplicitNullReturn(params CBObject[] args);

    private ObjectValue value;
    // will be set once after the first assignment
    public ValueType InitalType { get; private set; }
    public ValueType CurrentType => value.Type;
    private bool initalized;

    public CBObject(ObjectValue value) {
        this.value = value;
        if (value.Type != ValueType.Unset) {
            initalized = true;
            InitalType = this.value.Type;
        }
    }
    public CBObject(object value)
    : this(new ObjectValue(value)) { }
    public CBObject(CBFunction function)
    : this(new ObjectValue(new Func<CBObject[], CBObject>(function))) { }
    public CBObject(CBFunctionImplicitNullReturn function)
    : this(new ObjectValue(new Func<CBObject[], CBObject>(args => { function(args); return new(); }))) { }
    public CBObject()
    : this(ObjectValue.Unset) { }

    public CBObject ShallowCopy() => new(new ObjectValue(value));

    public virtual void SetValue(ObjectValue value) {
        if (!initalized) {
            InitalType = value.Type;
            initalized = true;
        }

        this.value = value.Cast(InitalType);
    }

    public virtual ObjectValue GetValue() => value;

    public override string ToString() => $"{value}";
}