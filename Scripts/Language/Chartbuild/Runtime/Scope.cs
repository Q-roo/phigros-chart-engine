using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Scope(Scope parent) {
    // object is CBObject.GetValue()
    private readonly Dictionary<object, CBObject> variables = [];
    public readonly Scope parent = parent;

    public CBObject this[object key] {
        get {
            GetVariableStore(key, out Dictionary<object, CBObject> variables);
            return variables[key];
        }
        set {
            GetVariableStore(key, out Dictionary<object, CBObject> variables);
            variables[key] = value;
        }
    }

    public void DeclareVariable(object key, CBObject value) {
        variables[key] = value;
    }

    // TODO: a better name
    // get the dictionary which contains the variable
    private bool GetVariableStore(object key, out Dictionary<object, CBObject> variables) {
        variables = this.variables;
        if (variables.ContainsKey(key))
            return true;
        else if (parent is not null && parent.GetVariableStore(key, out Dictionary<object, CBObject> parentVariables)) {
            variables = parentVariables;
            return true;
        }

        return false;
    }
}