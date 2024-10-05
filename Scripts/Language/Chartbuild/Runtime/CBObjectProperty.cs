namespace PCE.Chartbuild.Runtime;

public abstract class CBObjectPorperty: CBObject {
    public CBObjectPorperty()
    : base(ObjectValue.Property) {}

    public abstract override ObjectValue GetValue();
    public abstract override void SetValue(ObjectValue value);
}