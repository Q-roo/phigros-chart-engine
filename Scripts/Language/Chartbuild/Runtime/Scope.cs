using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class Scope(Scope parent) {
    private readonly Dictionary<string, CBVariable> varialbes = [];
    public Scope parent = parent;

    public Scope()
    : this(null) { }

    public Either<CBVariable, ErrorType> GetVariable(string name) {
        if (!varialbes.TryGetValue(name, out CBVariable value)) {
            if (parent is not null)
                return parent.GetVariable(name);
            return ErrorType.MissingMember;
        }
        return value;
    }

    public CBVariable GetVariableUnsafe(string name) {
        if (!varialbes.TryGetValue(name, out CBVariable value)) {
            if (parent is not null)
                return parent.GetVariableUnsafe(name);
            throw new UnreachableException();
        }
        return value;
    }
}