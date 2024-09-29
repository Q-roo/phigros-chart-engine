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

    // these should be only used where they are declared so no need to store any lookups
    private readonly List<byte[]> closureBodies = [];
    // the address is the address of the capture body
    private readonly Dictionary<Address, Address[]> captureLookup = [];

    public ChunkInfo() {

    }

    // make a shallow(-ish) copy
    // FIXME: the following is broken
    /*
    const a = 0;
    const inc = ||a++;
    inc();
    a;
    */
    // a also gets copied 
    // solution: do not copy
    // it works that way
    // even this
    /*
    const a = 0;
    const inc = |a|a++;
    inc(a);
    a;
    */
    // FIXME: currying
    // only make a copy of the variables in the parent chunk
    private ChunkInfo(ChunkInfo chunkInfo, params Address[] capture) {
        constantPool = chunkInfo.constantPool;
        constantAddressLookup = chunkInfo.constantAddressLookup;
        closureBodies = chunkInfo.closureBodies;
        captureLookup = chunkInfo.captureLookup;
        // these might be modifed so make a copy of them
        variables = new(chunkInfo.variables);
        variableAddressLookup = new(chunkInfo.variableAddressLookup);
        variableNameAddressLookup = new(chunkInfo.variableNameAddressLookup);

        foreach (Address address in capture) {
            CBObject variable = GetVariable(address);
            CBObject copy = variable.ShallowCopy();
            variables[address] = copy;

            Address variableAddress = variableAddressLookup[variable];
            variableAddressLookup.Remove(variable);
            variableAddressLookup[copy] = variableAddress;

            Address nameAddress = variableNameAddressLookup[variable];
            variableNameAddressLookup.Remove(variable);
            variableNameAddressLookup[copy] = nameAddress;
        }
    }

    public ChunkInfo Copy(params Address[] capture) => new(this, capture);

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

    public Address StoreClosureBody(byte[] body, /* ByteCodeChunk parent */ Address[] addresses) {
        Address address = (Address)closureBodies.Count;
        closureBodies.Add(body);
        // captureLookup[address] = parent.GetVariableAddresses();
        captureLookup[address] = addresses;


        return address;
    }

    public void UpdateClosureBody(Address address, byte[] body) => closureBodies[address] = body;

    public Address[] GetClosureCaptures(Address address) => captureLookup[address];
    public ByteCodeChunk GetClosure(Address address) => new(null, false, true, Copy(GetClosureCaptures(address))) { code = [.. closureBodies[address]] };
}