using System;
using System.Collections.Generic;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild.Bindings;

public delegate Property FallbackGetter(object key);
public delegate O ValueGetter();
public delegate void ValueSetter(O value);

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

    public NativeObjectBuilder AddConstantValue(object key, O value) => AddProperty(key, new ReadOnlyValueProperty(nativeObject, key, value));
    public NativeObjectBuilder AddReadOnlyValue(object key, ValueGetter getter) => AddProperty(key, new ReadOnlyProperty(nativeObject, key, (_, _) => getter()));

    public NativeObjectBuilder AddGetSetProperty(object key, ValueGetter getter, ValueSetter setter) => AddProperty(key, new SetGetProperty(nativeObject, key, (_ ,_) => getter(), (_, _, value) => setter(value)));

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