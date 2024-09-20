using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public class Scope(Scope parent) {
    private readonly Dictionary<string, CBVariable> varialbes = [];
    public Dictionary<string, CBVariable>.ValueCollection AllVariables => varialbes.Values;
    public Scope parent = parent;

    public Scope()
    : this(null) { }

    public bool HasVariable(string name) {
        if (varialbes.ContainsKey(name))
            return true;

        if (parent is not null)
            return parent.HasVariable(name);

        return false;
    }


    public ErrorType DeclareNonConstant(string name, bool @readonly, bool initalized) {
        if (HasVariable(name))
            return ErrorType.DuplicateIdentifier;

            varialbes.Add(name, new(@readonly, initalized, false));
        return ErrorType.NoError;
    }

    public ErrorType DeclareVariable(string name, ICBValue value, bool @readonly) {
        if (HasVariable(name))
            return ErrorType.DuplicateIdentifier;

        varialbes.Add(name, new(value, @readonly));
        return ErrorType.NoError;
    }

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