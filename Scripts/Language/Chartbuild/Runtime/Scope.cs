using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PCE.Chartbuild.Runtime;

using Value = O;

public class Scope : KVObject {
    public readonly Scope parent;
    public readonly ScopeRules rules;

    public Scope() {
        NativeValue = this;
        parent = null;
        rules = new();
    }

    public Scope(Scope parent)
    : this() {
        this.parent = parent;
        rules = new(parent.rules);
        rules.UpdateAspectRatio();
    }

    public override Property GetProperty(object key) {
        GetVariableStore(key, out Scope parent);
            if (parent.properties.TryGetValue(key, out Property property))
                return property;

            return parent.properties[key];
    }

    public void DeclareVariable(object key, Value value, bool @readonly) {
        AddProperty(key, @readonly ? new ReadOnlyValueProperty(this, key, value) : new ValueProperty(this, key, value));
    }

    // TODO: a better name
    // get the dictionary which contains the variable
    private bool GetVariableStore(object key, out Scope scope) {
        scope = this;
        if (scope.properties.ContainsKey(key))
            return true;
        else if (parent is not null && parent.GetVariableStore(key, out Scope parentScope)) {
            scope = parentScope;
            return true;
        }

        return false;
    }

    public override Value Copy(bool shallow = true, params object[] keys) {
        throw new UnreachableException("a scope should never get copied with \"Copy\"");
    }

    public override string ToString() => "scope";
}