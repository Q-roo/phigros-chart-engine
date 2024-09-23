namespace PCE.Chartbuild.Runtime;

using System.Collections.Generic;
using Address = ushort;

public class ByteCodeChunk(ByteCodeChunk parent, bool temporary) {
    public List<byte> code = new(200);
    private readonly ByteCodeChunk parent = parent;

    private static readonly List<object> constantPool = [];
    private static readonly Dictionary<object, Address> constantAddressLookup = [];
    private static readonly List<CBVariable> variables = [];

    private readonly Dictionary<string, CBVariable> variablesWithNames = [];
    private readonly Dictionary<CBVariable, Address> variableAddressLookup = [];
    private readonly Dictionary <string, ValueLink> variableLinks = [];

    public void MergeTemporary(ByteCodeChunk chunk) {
        foreach (string name in chunk.variablesWithNames.Keys) {
            CBVariable variable = chunk.variablesWithNames[name];

            variablesWithNames[name] = variable;
            variableAddressLookup[variable] = chunk.variableAddressLookup[variable];
            variableLinks[name] = chunk.variableLinks[name];
        }

        code.AddRange(parent.code);
    }

    public Address DeclareOrGet(string name, CBVariable variable) {
        if (TryGetLink(name, out ValueLink link))
            return link.Address;

        return DeclareVariable(name, variable);
    }

    public Address DeclareVariable(string name, CBVariable variable) {
        // there are cases when temporary chunks are created
        // while these are children of a chunk,
        //they shouldn't be allowed to have duplicate identifiers
        if (
            variablesWithNames.ContainsKey(name)
            || (temporary && parent.variablesWithNames.ContainsKey(name))
        )
            throw new System.Exception($"duplicate identifier: {name}");

        Address address = (Address)variables.Count;
        variables.Add(variable);
        variablesWithNames[name] = variable;
        variableAddressLookup[variable] = address;

        variableLinks[name] = new(this, name);

        return variableLinks[name].Address;
    }

    public Address Lookup(ValueLink link) {
        if (link.chunk != this)
            throw new System.Exception("link doesn't point to the right chunk");

        return variableAddressLookup[variablesWithNames[link.name]];
    }



    private bool TryGetLink(string name, out ValueLink link) {
        link = default;

        if (!variableLinks.TryGetValue(name, out ValueLink _)) {
            if (parent is null)
                return false;

            return parent.TryGetLink(name, out link);
        }

        return true;
    }

    private ValueLink GetLink(string name) {
        if (!TryGetLink(name, out ValueLink link))
            throw new System.Exception($"missing member: {name}");

        return link;
    }

    public Address Lookup(string name) {
        if (!variableLinks.TryGetValue(name, out ValueLink value)) {
            ValueLink link = GetLink(name);

            value = new(link.chunk, name);
            variableLinks[name] = value;
        }

        return value.Address;
    }

    public static Address AddConstant(object constant) {
        constantPool.Add(constant);
        Address address = (Address)(constantPool.Count - 1);
        constantAddressLookup[constant] = address;
        return address;
    }

    public static bool HasConstant(object constant) {
        return constantAddressLookup.ContainsKey(constant);
    }

    public static Address ConstantLookup(object constant) {
        return constantAddressLookup[constant];
    }
}