using System;
using System.Collections.Generic;
using Godot;
using PCE.Chartbuild.Runtime;
using Callable = PCE.Chartbuild.Runtime.Callable;

namespace PCE.Chartbuild.Bindings;

public delegate Property FallbackGetter(object key);
public delegate Runtime.Object ValueGetter();
public delegate void ValueSetter(Runtime.Object value);
public delegate Runtime.Object SetValueTransformer(Runtime.Object value);
public delegate T ValueGetter<T>();
public delegate void OnChange<T>(T value);

// TODO: generic type parameter constraint similar to how godot does it with variant
// Variant.From<Callable>(null) -> The generic type argument 'PCE.Chartbuild.Runtime.Callable' must be a Variant compatible type
// https://docs.godotengine.org/en/4.3/tutorials/scripting/c_sharp/diagnostics/GD0301.html
// https://github.com/godotengine/godot/blob/master/modules/mono/glue/GodotSharp/GodotSharp/Core/Attributes/MustBeVariantAttribute.cs
[AttributeUsage(AttributeTargets.GenericParameter)]
public class MustSupportToObjectCastAttribute : Attribute {

}

public class NativeObjectBuilder {
    private readonly Dictionary<object, Property> properties;
    private FallbackGetter fallbackGetter;
    private readonly NativeObject nativeObject;

    public NativeObjectBuilder(object nativeValue) {
        properties = [];
        nativeObject = new(nativeValue, key => {
            if (properties.TryGetValue(key, out Property property))
                return property;

            if (fallbackGetter is not null)
                return fallbackGetter(key);

            throw new KeyNotFoundException($"unknown key \"{key}\"");
        });
    }

    public NativeObjectBuilder AddProperty(object key, Property property) {
        properties[key] = property;
        return this;
    }

    public NativeObjectBuilder AddConstantValue(object key, Runtime.Object value) => AddProperty(key, new ReadOnlyValueProperty(nativeObject, key, value));
    public NativeObjectBuilder AddReadOnlyValue(object key, ValueGetter getter) => AddProperty(key, new ReadOnlyProperty(nativeObject, key, (_, _) => getter()));

    private NativeObjectBuilder AddGetSetProperty(object key, ValueGetter getter, ValueSetter setter) => AddProperty(key, new SetGetProperty(nativeObject, key, (_, _) => getter(), (_, _, value) => setter(value)));

    private NativeObjectBuilder AddChangeableProperty(object key, ValueGetter getter, SetValueTransformer valueTransformer, Runtime.Object.OnChange onChange) {
        Runtime.Object cached= null;
        return AddGetSetProperty(key, () => {
            cached = getter();
            cached.OnValueChanged += onChange;
            return cached;
        }, value => {
            if (cached is not null)
                cached.SetNativeValue(valueTransformer(value));
            else
                {
                    GD.PushWarning($"property \"{key}\" was set before the value was cached on {nativeObject}");
                    onChange(null, valueTransformer(value).NativeValue);
                }
        });
    }

    // this propbably goes against all of the good practices but who cares, it is convenient to use
    public NativeObjectBuilder AddChangeableProperty<[MustSupportToObjectCast] T>(object key, ValueGetter<T> getter, OnChange<T> onChange) => typeof(T) switch {
        Type t when t == typeof(int) => AddChangeableProperty(key, () => (int)(object)getter(), value => (Runtime.Object)(int)value, (_, value) => onChange((T)value)),
        Type t when t == typeof(float) => AddChangeableProperty(key, () => (float)(object)getter(), value => (Runtime.Object)(float)value, (_, value) => onChange((T)value)),
        Type t when t == typeof(bool) => AddChangeableProperty(key, () => (bool)(object)getter(), value => (Runtime.Object)(bool)value, (_, value) => onChange((T)value)),
        Type t when t == typeof(string) => AddChangeableProperty(key, () => (string)(object)getter(), value => (Runtime.Object)(string)value, (_, value) => onChange((T)value)),
        Type t when t == typeof(Vector2) => AddChangeableProperty(key, () => (Vector2)(object)getter(), value => (Runtime.Object)(Vector2)value, (_, value) => onChange((T)value)),
        _ => throw new InvalidCastException($"{typeof(T)} cannot be turned into {typeof(Runtime.Object)}")
    };

    private NativeObjectBuilder SetFallbackGetter(FallbackGetter fallbackGetter) {
        this.fallbackGetter = fallbackGetter;
        return this;
    }

    public NativeObjectBuilder SetFallbackGetter(Func<NativeObject, FallbackGetter> createFallbackGetter) => SetFallbackGetter(createFallbackGetter(nativeObject));

    public NativeObjectBuilder AddCallable(object key, Callable callable) => AddConstantValue(key, callable);
    public NativeObjectBuilder AddCallable(object key, CallFunction function) => AddCallable(key, new Callable(function));
    public NativeObjectBuilder AddCallable(object key, CallFunctionImplicitNullReturn function) => AddCallable(key, new Callable(function));

    public NativeObject Build() => nativeObject;
}