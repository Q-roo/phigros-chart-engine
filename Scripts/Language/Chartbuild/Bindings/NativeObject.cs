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

    public override Object Copy(bool shallow = true, params object[] keys) {
        // TODO: support shallow copy
        return new NativeObject(value, propertyGetter);
    }

    public override Object BinaryOperation(OperatorType @operator, Object rhs) {
        return @operator switch {
            OperatorType.Equal => Equals(rhs),
            OperatorType.NotEqual => !Equals(rhs),
            _ => base.BinaryOperation(@operator, rhs)
        };
    }

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Not => NativeValue is null,
        _ => base.UnaryOperation(@operator, prefix)
    };

    public override string ToString() {
        return value.ToString();
    }
}