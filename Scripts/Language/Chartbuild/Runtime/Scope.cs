using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

using Value = Object;

public class Scope(Scope parent) {
    // object is CBObject.GetValue()
    private readonly Dictionary<object, Value> variables = [];
    public readonly Scope parent = parent;

    public Value this[object key] {
        get {
            GetVariableStore(key, out Dictionary<object, Value> variables);
            return variables[key];
        }
        set {
            GetVariableStore(key, out Dictionary<object, Value> variables);
            variables[key] = value;
        }
    }

    public void DeclareVariable(object key, Value value) {
        variables[key] = value;
    }

    // TODO: a better name
    // get the dictionary which contains the variable
    private bool GetVariableStore(object key, out Dictionary<object, Value> variables) {
        variables = this.variables;
        if (variables.ContainsKey(key))
            return true;
        else if (parent is not null && parent.GetVariableStore(key, out Dictionary<object, Value> parentVariables)) {
            variables = parentVariables;
            return true;
        }

        return false;
    }
}