using System.Collections.Generic;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild.Bindings;

using Object = O;

public class NativeObject(object value, Dictionary<object, Property> properties) : Object(value) {
    private readonly object value = value;
    private readonly Dictionary<object, Property> properties = properties;

    public NativeObject(object value)
    : this(value, []) { }

    public override Object Copy(bool shallow = true, params object[] keys) {
        Dictionary<object, Property> properties = shallow ? this.properties : new(this.properties);
        
        foreach (object key in keys) {
            properties[key] = (Property)this.properties[key].Copy(!shallow);
        }
        return new NativeObject(value, properties);
    }

    public override Object BinaryOperation(OperatorType @operator, Object rhs) {
        return @operator switch {
            OperatorType.Equal => Equals(rhs),
            OperatorType.NotEqual => !Equals(rhs),
            _ => base.BinaryOperation(@operator, rhs)
        };
    }

    public override Object UnaryOperation(OperatorType @operator, bool prefix) => @operator switch {
        OperatorType.Not => nativeValue is null,
        _ => base.UnaryOperation(@operator, prefix)
    };

    public override string ToString() {
        return value.ToString();
    }
}