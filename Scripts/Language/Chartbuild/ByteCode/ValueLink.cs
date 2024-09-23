namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public class ValueLink(ByteCodeChunk chunk, string name) {
    public readonly ByteCodeChunk chunk = chunk;
    public readonly string name = name;
    public Address Address => chunk.Lookup(this);
}