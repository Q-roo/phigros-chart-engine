namespace PCE.Chartbuild.Runtime;

public class DefaultObjectPropertyDescriptor(ObjectValue value) : CBObjectPropertyDescriptor {
    public ObjectValue value = value;
    public override ObjectValue GetValue() => value;
    public override void SetValue(ObjectValue value) => this.value = value;
}