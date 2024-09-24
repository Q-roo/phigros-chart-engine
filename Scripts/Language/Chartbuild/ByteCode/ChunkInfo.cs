using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public class ChunkInfo {
    // for now strings, but eventually, ICBValues
    private readonly List<object> constantPool = [];
    private readonly Dictionary<object, Address> constantAddressLookup = [];

    private readonly List<CBVariable> variables = [];
    private readonly Dictionary<CBVariable, Address> variableAddressLookup = [];
    private readonly Dictionary<CBVariable, Address> variableNamesAddresses = [];

    public Address CreateVariable(string name, CBVariable variable) {
        // will throw an exception if the key already ecists
        variableAddressLookup.Add(variable, (Address)variables.Count);
        variables.Add(variable);
        Address address = AddOrGetConstant(name);
        variableNamesAddresses[variable] = address;

        return variableAddressLookup[variable];
    }

    public bool HasVariable(CBVariable variable) => variableAddressLookup.ContainsKey(variable);

    public Address AddOrGetConstant(object constant) {
        if (constantAddressLookup.TryGetValue(constant, out Address address))
            return address;

        address = (Address)constantPool.Count;
        constantPool.Add(constant);
        constantAddressLookup[constant] = address;

        return address;
    }

    public CBVariable GetVariable(Address address) => variables[address];

    public object GetConstant(Address address) => constantPool[address];
    public string GetVariableName(CBVariable variable) => (string)GetConstant(variableNamesAddresses[variable]);
    public string GetVariableName(Address address) => GetVariableName(GetVariable(address));
}