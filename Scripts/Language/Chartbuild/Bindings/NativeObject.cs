using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild.Bindings;

using Object = O;

public delegate Property PropertyGetter(object key);

public class NativeObject(object value, PropertyGetter propertyGetter) : Object(value) {
    private readonly object value = value;
    private readonly PropertyGetter propertyGetter = propertyGetter;

    public NativeObject(object value)
    : this(value, null) {
        propertyGetter = key => throw KeyNotFound(key);
    }

    public override Property GetProperty(object key) => propertyGetter(key);

    public override Object Copy(bool shallow = true, params object[] keys) {
        // TODO: support shallow copy
        return new NativeObject(value, propertyGetter);
    }

    public override string ToString() {
        return value.ToString();
    }

    public override bool ToBool() => value is not null;
}