using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class KVObject() : Object(null) {
    protected readonly Dictionary<object, Property> properties = [];
    public override Property GetProperty(object key) => properties[key];

    public void AddProperty(object key, Property property) {
        properties[key] = property;
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        throw new System.NotImplementedException();
    }
}