namespace PCE.Chartbuild.Runtime;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Address = ushort;

public class UnsafeVM(ByteCodeChunk chunk) {
    private readonly ByteCodeChunk chunk = chunk;
    private ChunkInfo ChunkInfo => chunk.info;

    private int programCounter;

    private readonly Stack<CBObject> stack = new(200);

    public object Run() {
        Reset();
        while (programCounter < chunk.code.Count) {
            UnsafeOpCode opCode = (UnsafeOpCode)Read();
            Godot.GD.Print($"{programCounter}: {opCode}");
            switch (opCode) {
                case UnsafeOpCode.HLT:
                    return stack.Pop().GetValue();
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
        stack.Push(new());
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
                // chunkInfo.GetVariable(ReadAddress()).SetValue(new());
                // the value is already set to unset
                ReadAddress(); // TODO: maybe reset it
                break;
            case UnsafeOpCode.ASGN: { // a
                // b
                // asgn
                CBObject b = stack.Pop();
                stack.Pop().SetValue(b.GetValue());
                break;
            }
            case UnsafeOpCode.DSPA:
                // TODO: new value type
                stack.Push(new(ReadAddress()));
                break;
            case UnsafeOpCode.DSPI:
                stack.Push(new(ReadI32()));
                break;
            case UnsafeOpCode.DSPD:
                stack.Push(new(ReadF32()));
                break;
            case UnsafeOpCode.DSPB:
                stack.Push(new(ReadBool()));
                break;
            case UnsafeOpCode.DSPN:
                stack.Push(new());
                break;
            case UnsafeOpCode.SPOP:
                stack.Pop();
                break;
            case UnsafeOpCode.LCST:
                stack.Push(ChunkInfo.GetConstant(ReadAddress()) switch {
                    string str => new(str),
                    // for now, only strings are stored there
                    _ => throw new UnreachableException()
                });
                break;
            case UnsafeOpCode.ACOL:
                int size = ReadI32();
                List<ObjectValue> values = new(size);
                for (int i = 0; i < size; i++)
                    values.Add(stack.Pop().GetValue());

                values.Reverse();
                stack.Push(new(new ObjectValueArray(values)));
                break;
            case UnsafeOpCode.TRAN:
            case UnsafeOpCode.TRANI:
                // TODO
                throw new NotImplementedException();
            case UnsafeOpCode.BINOP: {
                TokenType @operator = (TokenType)Read();
                CBObject b = stack.Pop();
                stack.Push(new(stack.Pop().GetValue().ExecuteBinaryOperator(@operator, b.GetValue())));
                break;
            }
            case UnsafeOpCode.PREOP:
            case UnsafeOpCode.POSOP: {
                TokenType @operator = (TokenType)Read();
                stack.Push(new(stack.Pop().GetValue().ExecuteUnaryOperator(@operator)));
                break;
            }
            case UnsafeOpCode.CALL:
            case UnsafeOpCode.CALLN:
                throw new NotImplementedException();
            // case UnsafeOpCode.IGET:
            // TODO: bring back scopes
            // throw new NotImplementedException();
            case UnsafeOpCode.LDV:
                stack.Push(ChunkInfo.GetVariable(ReadAddress()));
                break;
            case UnsafeOpCode.MGET: {
                CBObject b = stack.Pop();
                stack.Push(new(stack.Pop().GetValue().members[b.GetValue().value].Get()));
                break;
            }
            case UnsafeOpCode.JMP: {
                programCounter = stack.Pop().GetValue().AsInt();
                break;
            }
            case UnsafeOpCode.JMPI: {
                int address = stack.Pop().GetValue().AsInt();
                bool condition = stack.Pop().GetValue().AsBool();

                if (condition)
                    programCounter = address;
                break;
            }
            case UnsafeOpCode.JMPN: {
                int address = stack.Pop().GetValue().AsInt();
                bool condition = stack.Pop().GetValue().AsBool();

                if (!condition)
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