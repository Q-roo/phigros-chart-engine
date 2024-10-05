namespace PCE.Chartbuild.Runtime;

public class DefaultObjectPropertyDescriptor(ObjectValue value) : CBObjectPorperty {
    public ObjectValue value = value;
    public override ObjectValue GetValue() => value;
    public override void SetValue(ObjectValue value) => this.value = value;
}