using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

public class Scope(Scope parent) {
    private readonly Dictionary<dynamic, dynamic> variables = [];
    public readonly Scope parent = parent;

    public dynamic this[dynamic key] {
        get {
            if (!variables.TryGetValue(key, out dynamic result))
                return parent[key];
            return result;
        }
        set {
            if (!variables.ContainsKey(key) && parent is not null)
                parent[key] = value;
            else
                variables[key] = value;
        }
    }
}