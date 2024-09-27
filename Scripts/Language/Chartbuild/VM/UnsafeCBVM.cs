namespace PCE.Chartbuild.Runtime;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Address = ushort;

public class UnsafeVM(ByteCodeChunk chunk) {
    private readonly ByteCodeChunk chunk = chunk;
    private ChunkInfo chunkInfo => chunk.info;

    private int programCounter;

    private readonly Stack<CBObject> stack = new(200);

    public ICBValue Run() {
        Reset();
        while (programCounter < chunk.code.Count) {
            UnsafeOpCode opCode = (UnsafeOpCode)Read();
            switch (opCode) {
                case UnsafeOpCode.HLT:
                    return stack.Pop().Value;
                default:
                    ExecuteInstruction(opCode);
                    break;
            }
        }

        throw new UnreachableException();
    }

    private void Reset() {
        programCounter = 0;
        stack.Clear();
        stack.Push(new(new NullValue()));
    }

    private byte Read() => chunk.code[programCounter++];

    private byte[] ReadN(int size) {
        byte[] bytes= new byte[size];
        for (int i = 0; i < size; i++)
            bytes[i] = Read();

        return bytes;
    }

    private T ReadT<T>() where T : struct => MemoryMarshal.Read<T>(ReadN(Marshal.SizeOf<T>()));

    private Address ReadAddress() => MemoryMarshal.Read<Address>(ReadN(sizeof(Address)));
    private double ReadF32() => MemoryMarshal.Read<double>(ReadN(sizeof(double)));
    private int ReadI32() => MemoryMarshal.Read<int>(ReadN(sizeof(int)));
    private bool ReadBool() => MemoryMarshal.Read<bool>(ReadN(sizeof(bool)));

    // the last opcode is hlt
    private void ForceExit() => programCounter = chunk.code.Count - 1;

    private void ExecuteInstruction(UnsafeOpCode opCode) {
        switch (opCode) {
            case UnsafeOpCode.HLT:
            case UnsafeOpCode.NOOP:
                return;
            case UnsafeOpCode.DCLV:
                // the variables are already created so just assign a default value
                chunkInfo.GetVariable(ReadAddress()).SetValueUnsafe(new CBObject(new NullValue()));
                break;
            case UnsafeOpCode.ASGN: { // a
                // b
                // asgn
                ICBValue b = stack.Pop().Value;
                stack.Pop().SetValue(b);
                break;
            }
            case UnsafeOpCode.DSPA:
                // TODO: new value type
                stack.Push(new(new I32Value(ReadAddress())));
                break;
            case UnsafeOpCode.DSPI:
                stack.Push(new(new I32Value(ReadI32())));
                break;
            case UnsafeOpCode.DSPD:
                stack.Push(new(new F32Value(ReadF32())));
                break;
            case UnsafeOpCode.DSPB:
                stack.Push(new(new BoolValue(ReadBool())));
                break;
            case UnsafeOpCode.DSPN:
                stack.Push(new(new NullValue()));
                break;
            case UnsafeOpCode.SPOP:
                stack.Pop();
                break;
            case UnsafeOpCode.LCST:
                stack.Push(chunkInfo.GetConstant(ReadAddress()) switch {
                    string str => new(new StringValue(str)),
                    // for now, only strings are stored there
                    _ => throw new UnreachableException()
                });
                break;
            case UnsafeOpCode.ACOL:
                int size = ReadI32();
                List<ICBValue> values = new(size);
                for (int i = 0; i < size; i++)
                    values.Add(stack.Pop().Value);

                values.Reverse();
                stack.Push(new(new ArrayValue() { values = values }));
                break;
            case UnsafeOpCode.TRAN:
            case UnsafeOpCode.TRANI:
                // TODO
                throw new NotImplementedException();
            case UnsafeOpCode.BINOP: {
                TokenType @operator = (TokenType)Read();
                ICBValue b = stack.Pop().Value;
                stack.Push(new(stack.Pop().ExecuteBinaryOperatorUnsafe(@operator, b)));
                break;
            }
            case UnsafeOpCode.PREOP:
            case UnsafeOpCode.POSOP:
                // TODO
                throw new NotImplementedException();
            case UnsafeOpCode.CALL:
            case UnsafeOpCode.CALLN:
                throw new NotImplementedException();
            // case UnsafeOpCode.IGET:
                // TODO: bring back scopes
                // throw new NotImplementedException();
                case UnsafeOpCode.LDV:
                stack.Push(new(chunkInfo.GetVariable(ReadAddress()).GetValueUnsafe()));
                break;
            case UnsafeOpCode.MGET: {
                ICBValue b = stack.Pop().Value;
                stack.Push(new(stack.Pop().GetMemberUnsafe(b)));
                break;
            }
            case UnsafeOpCode.JMP: {
                programCounter = ((I32Value)stack.Pop().Value).value;
                break;
            }
            case UnsafeOpCode.JMPI: {
                int address = ((I32Value)stack.Pop().Value).value;
                BoolValue condition = new BoolType().Constructor(stack.Pop().Value).Case switch {
                    BoolValue v => v,
                    _ => throw new UnreachableException()
                };

                if ((bool)condition)
                    programCounter = address;
                break;
            }
            case UnsafeOpCode.JMPN: {
                int address = ((I32Value)stack.Pop().Value).value;
                BoolValue condition = new BoolType().Constructor(stack.Pop().Value).Case switch {
                    BoolValue v => v,
                    _ => throw new UnreachableException()
                };

                if (!(bool)condition)
                    programCounter = address;
                break;
            }
            case UnsafeOpCode.RET:
            case UnsafeOpCode.ITER:
            case UnsafeOpCode.ITERN:
                throw new NotImplementedException();
            case UnsafeOpCode.LSTART:
            case UnsafeOpCode.LEND:
                break;
            case UnsafeOpCode.CSTART:
            case UnsafeOpCode.CEND:
            case UnsafeOpCode.CPTR:
                throw new NotImplementedException();
            default:
                throw new Exception($"unknown opcode: {opCode}");
        }
    }
}