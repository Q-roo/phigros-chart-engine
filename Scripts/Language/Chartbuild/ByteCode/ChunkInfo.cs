using System.Collections.Generic;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public class ChunkInfo {
    // for now strings, but eventually, ICBValues
    private readonly List<object> constantPool = [];
    private readonly Dictionary<object, Address> constantAddressLookup = [];

    private readonly List<CBObject> variables = [];
    private readonly Dictionary<CBObject, Address> variableAddressLookup = [];
    private readonly Dictionary<CBObject, Address> variableNameAddressLookup = [];

    public ChunkInfo() {

    }

    // make a shallow(-ish) copy
    private ChunkInfo(ChunkInfo chunkInfo) {
        constantPool = new(chunkInfo.constantPool);
        constantAddressLookup = new(chunkInfo.constantAddressLookup);

        foreach ((CBObject key, Address address) in chunkInfo.variableAddressLookup)
            variableAddressLookup[key.ShallowCopy()] = address;

        variables.Capacity = chunkInfo.variables.Count;
        foreach (CBObject variable in chunkInfo.variables) {
            CBObject copy = variable.ShallowCopy();
            variables.Add(copy);
            variableAddressLookup[copy] = chunkInfo.variableAddressLookup[variable];
            variableNameAddressLookup[copy] = chunkInfo.variableNameAddressLookup[variable];
        }
    }

    public ChunkInfo Copy() => new(this);

    public Address CreateVariable(string name, CBObject variable) {
        // will throw an exception if the key already exists
        variableAddressLookup.Add(variable, (Address)variables.Count);
        variables.Add(variable);
        Address address = AddOrGetConstant(name);
        variableNameAddressLookup[variable] = address;

        return variableAddressLookup[variable];
    }

    public bool HasVariable(CBObject variable) => variableAddressLookup.ContainsKey(variable);

    public Address AddOrGetConstant(object constant) {
        if (constantAddressLookup.TryGetValue(constant, out Address address))
            return address;

        address = (Address)constantPool.Count;
        constantPool.Add(constant);
        constantAddressLookup[constant] = address;

        return address;
    }

    public CBObject GetVariable(Address address) => variables[address];
    public object GetConstant(Address address) => constantPool[address];
    public string GetVariableName(CBObject variable) => (string)GetConstant(variableNameAddressLookup[variable]);
    public string GetVariableName(Address address) => GetVariableName(GetVariable(address));
}