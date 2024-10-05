using System;
using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

using Value = Object;

public class Scope : Value {
    private readonly Dictionary<object, Value> variables;
    private readonly HashSet<object> readOnlyVariableKeys;
    public readonly Scope parent;
    public readonly ScopeRules rules;

    public Scope() {
        parent = null;
        rules = new();
        variables = [];
        readOnlyVariableKeys = [];
    }

    public Scope(Scope parent)
    : this() {
        this.parent = parent;
        rules = new(parent.rules);
        rules.UpdateAspectRatio();
    }

    public override object Value => this;

    public override Value this[object key] {
        get {
            GetVariableStore(key, out Scope parent);
            return parent.variables[key];
        }
        set {
            GetVariableStore(key, out Scope parent);
            if (parent.readOnlyVariableKeys.Contains(key))
                throw ReadOnlyProperty(key);

            parent.variables[key] = value;
            value.parentKey = key;
            value.parentObject = parent;
        }
    }

    public void DeclareVariable(object key, Value value, bool @readonly) {
        variables[key] = value;
        value.parentKey = key;
        value.parentObject = this;

        if (@readonly)
            readOnlyVariableKeys.Add(key);
    }

    // TODO: a better name
    // get the dictionary which contains the variable
    private bool GetVariableStore(object key, out Scope scope) {
        scope = this;
        if (scope.variables.ContainsKey(key))
            return true;
        else if (parent is not null && parent.GetVariableStore(key, out Scope parentScope)) {
            scope = parentScope;
            return true;
        }

        return false;
    }

    public override Value Copy(bool shallow = true, params object[] keys) {
        throw new NotImplementedException();
    }

    public override Value Call(params Value[] args) {
        throw NotCallable();
    }

    public override Value ExecuteBinary(OperatorType @operator, Value rhs) {
        throw NotSupportedOperator(@operator);
    }

    public override Value ExecuteUnary(OperatorType @operator, bool prefix) {
        throw NotSupportedOperator(@operator);
    }

    public override IEnumerator<Value> GetEnumerator() {
        throw NotIterable();
    }

    public override string ToString() => "scope";
}