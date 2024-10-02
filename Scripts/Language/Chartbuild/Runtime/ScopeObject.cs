using System;
using System.Collections;

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

// public interface IScopeObjectProperty {
//     public ScopeObject Object {get;}
//     public ScopeObject Get();
//     public ScopeObject Set(ScopeObject value);
// }

public abstract class ScopeObject {
    protected abstract object Value {get; set;}
    // refers to the inner type when it's an array
    public ObjectType type;
    public bool isArray;

    public void SetValue(ScopeObject value) {
        Value = value.Value;
        type = value.type;
        isArray = value.isArray;
    }

    public object GetValue() => Value;

    public ScopeObject this[ScopeObject key] {
        get => GetMember(key);
        set => SetMember(key, value);
    }

    public abstract ScopeObject GetMember(ScopeObject key);
    public abstract ScopeObject SetMember(ScopeObject key, ScopeObject value);

    public abstract ScopeObject BinaryOperator(TokenType @operator, ScopeObject rhs);
    public abstract ScopeObject UnaryOperator(TokenType @operator);

    public IEnumerable AsIterable() {
        return (IEnumerable)Value;
    }
}

public class I32Value : ScopeObject {
    private int value;

    protected override object Value {
        get => value;
        set => this.value = (int)value;
    }

    public override ScopeObject BinaryOperator(TokenType @operator, ScopeObject rhs) {
        throw new NotImplementedException();
    }

    public override ScopeObject GetMember(ScopeObject key) {
        throw new NotSupportedException();
    }

    public override ScopeObject SetMember(ScopeObject key, ScopeObject value) {
        throw new NotSupportedException();
    }

    public override ScopeObject UnaryOperator(TokenType @operator) {
        throw new System.NotImplementedException();
    }
}