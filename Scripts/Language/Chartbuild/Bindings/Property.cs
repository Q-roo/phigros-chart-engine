using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild.Bindings;

public abstract class Property<T> : Object {
    public abstract T value { get; set; }
    protected T cached;

    public override object Value => value;

    public override string ToString() {
        return value.ToString();
    }
}

public delegate T Getter<T>();
public delegate void Setter<T>(T value);
public delegate void KeySetter(object key, Object @this, Object value);
public class HijackedSetter(Object @object, KeySetter setter) : Object {
    private readonly Object @object = @object;
    private readonly KeySetter setter = setter;

    public override Object this[object key] { get => @object[key]; set => setter(key, @object, value); }

    public override object Value => @object.Value;

    public override Object Call(params Object[] args) {
        return @object.Call(args);
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return @object.Copy(shallow, keys);
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        return @object.ExecuteBinary(@operator, rhs);
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return @object.ExecuteUnary(@operator, prefix);
    }

    public override IEnumerator<Object> GetEnumerator() {
        return @object.GetEnumerator();
    }

    public override string ToString() {
        return @object.ToString();
    }
}

public class ObjectProperty(Getter<Object> getter, Setter<Object> setter) : Property<Object> {

    private readonly Getter<Object> getter = getter;
    private readonly Setter<Object> setter = setter;

    public override Object this[object key] {
        get => value[key];
        set {
            this.value[key] = value;
            setter(this.value);
        }
    }

    private HijackedSetter CreateCached() {
        Object @object = getter();
        parentObject = @object.parentObject;
        parentKey = @object.parentKey;
        @object.parentObject = this;
        return new(@object, (key, @this, value) => {
            GD.Print($"{@this}[{key}]={value}");
            @this[key] = value;
            value = @this;
        });
    }

    public override Object value {
        get => CreateCached();
        set {
            setter(value);
            cached = CreateCached();
        }
    }

    public override Object Call(params Object[] args) {
        return value.Call(args);
    }

    public override Object Copy(bool shallow = true, params object[] keys) {
        return value.Copy(shallow, keys);
    }

    public override Object ExecuteBinary(OperatorType @operator, Object rhs) {
        return value.ExecuteBinary(@operator, rhs);
    }

    public override Object ExecuteUnary(OperatorType @operator, bool prefix) {
        return value.ExecuteUnary(@operator, prefix);
    }

    public override IEnumerator<Object> GetEnumerator() {
        return value.GetEnumerator();
    }
}

public class Vec2Property(Getter<Vector2> getter, Setter<Vector2> setter) : ObjectProperty(() => new Vec2(getter()), value => setter(value.ToVec2().value)) {

}