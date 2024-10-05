namespace PCE.Chartbuild.Runtime;

public class CBObject {
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
    : this(new(value)) { }
    public CBObject()
    : this(ObjectValue.Unset) { }

    public CBObject ShallowCopy() => new(new(value));

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