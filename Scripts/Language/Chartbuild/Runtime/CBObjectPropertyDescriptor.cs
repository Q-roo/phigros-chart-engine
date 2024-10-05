namespace PCE.Chartbuild.Runtime;

public abstract class CBObjectPropertyDescriptor : CBObject {
    public abstract override ObjectValue GetValue();
    public abstract override void SetValue(ObjectValue value);
}