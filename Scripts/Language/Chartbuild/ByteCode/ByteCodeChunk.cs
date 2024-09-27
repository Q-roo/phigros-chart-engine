namespace PCE.Chartbuild.Runtime;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Address = ushort;

public class ByteCodeChunk(ByteCodeChunk parent, bool temporary, ChunkInfo info) {
    public List<byte> code = new(200);
    private readonly ByteCodeChunk parent = parent;
    public readonly ChunkInfo info = info;

    private readonly Dictionary<string, CBObject> variablesWithNames = [];
    private readonly Dictionary<CBObject, Address> variableAddressLookup = [];
    private readonly Dictionary <string, ValueLink> variableLinks = [];

    public void MergeTemporary(ByteCodeChunk chunk) {
        foreach (string name in chunk.variablesWithNames.Keys) {
            CBObject variable = chunk.variablesWithNames[name];

            variablesWithNames[name] = variable;
            variableAddressLookup[variable] = chunk.variableAddressLookup[variable];
            variableLinks[name] = chunk.variableLinks[name];
        }

        Merge(chunk);
    }

    public void Merge(ByteCodeChunk chunk) {
        // correct the addresses
        for (int i = 0; i < chunk.code.Count;) {
            UnsafeOpCode instruction = (UnsafeOpCode)chunk.code[i];
            switch (instruction) {
                case UnsafeOpCode.DSPA:
                    // // get the address
                    i++;
                    Address address = BitConverter.ToUInt16(CollectionsMarshal.AsSpan(chunk.code.Slice(i, sizeof(Address))));
                    // // correct the address
                    address += (Address)code.Count;
                    // // remove the wrong address
                    chunk.code.RemoveRange(i, sizeof(Address));
                    // // insert the correct address back
                    chunk.code.InsertRange(i, BitConverter.GetBytes(address));
                    i += instruction.SizeOf() - 1;
                    break;
                default:
                    i += instruction.SizeOf();
                    break;
            }
        }
        code.AddRange(chunk.code);
    }

    public void ResloveLoopLabels() {
        int start = 0;
        int end = 0;
        for (int i = 0; i < code.Count;) {
            UnsafeOpCode instruction = (UnsafeOpCode)code[i];

            switch (instruction) {
                case UnsafeOpCode.JMPE:
                case UnsafeOpCode.JMPNE:
                case UnsafeOpCode.JMPS: {
                    code[i] = UnsafeOpCode.DSPA.AsByte();
                    byte[] address = BitConverter.GetBytes(instruction == UnsafeOpCode.JMPS ? start : end);
                    for (int j = 0; j < sizeof(Address); j++)
                        code[i + 1 + j] = address[j];
                    code[i + sizeof(Address) + 1] = (instruction == UnsafeOpCode.JMPNE ? UnsafeOpCode.JMPN : UnsafeOpCode.JMP).AsByte();
                    i += UnsafeOpCode.DSPA.SizeOf() + (instruction == UnsafeOpCode.JMPNE ? UnsafeOpCode.JMPN : UnsafeOpCode.JMP).SizeOf();
                    break;
                }
                // cannot remove this 2 since that would mean the dspa addresses would have to be fixed once again
                case UnsafeOpCode.LSTART: {
                    // without the +1, it would jump to the end of the previous instruction, not to the label
                    start = i + 1;
                    for (int j = i + UnsafeOpCode.LSTART.SizeOf(); j < code.Count;) {
                        UnsafeOpCode opCode = (UnsafeOpCode)code[j];
                        if (opCode == UnsafeOpCode.LEND) {
                            end = j; // this will jump to the label
                            break;
                        }

                        j += opCode.SizeOf();
                    }
                    i += instruction.SizeOf();
                    break;
                }
                case UnsafeOpCode.LEND:
                    // end = i;
                    i += instruction.SizeOf();
                    break;
                default:
                    i += instruction.SizeOf();
                    break;
            }
        }
    }

    public Address DeclareOrGet(string name, CBObject variable) {
        if (TryGetLink(name, out ValueLink link))
            return link.Address;

        return DeclareVariable(name, variable);
    }

    public Address DeclareVariable(string name, CBObject variable) {
        // there are cases when temporary chunks are created
        // while these are children of a chunk,
        //they shouldn't be allowed to have duplicate identifiers
        if (
            variablesWithNames.ContainsKey(name)
            || (temporary && parent.variablesWithNames.ContainsKey(name))
        )
            throw new Exception($"duplicate identifier: {name}");

        Address address = info.CreateVariable(name, variable);
        variablesWithNames[name] = variable;
        variableAddressLookup[variable] = address;

        variableLinks[name] = new(this, name);

        return variableLinks[name].Address;
    }

    public Address Lookup(ValueLink link) {
        if (link.chunk != this)
            throw new Exception("link doesn't point to the right chunk");

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
            throw new Exception($"missing member: {name}");

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

    public Address AddOrGetConstant(object constant) {
        return info.AddOrGetConstant(constant);
    }
}