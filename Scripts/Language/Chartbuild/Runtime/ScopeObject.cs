namespace PCE.Chartbuild.Runtime;

public enum ObjectType {
    Unset,
    I32,
    F32,
    Str,
    Bool,
    Callable,
    Object
}

public class ScopeObject {
    private object value;
    // refers to the inner type when it's an array
    public ObjectType type;
    public bool isArray;

    public void SetValue(ScopeObject value) {
        this.value = value.value;
        type = value.type;
        isArray = value.isArray;
    }

    public object GetValue() => value;

    public IEnumerable AsIterable() {
        return (IEnumerable)value;
    }
}