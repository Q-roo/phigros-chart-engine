using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LanguageExt.UnsafeValueAccess;

namespace PCE.Chartbuild.Runtime;

using Address = UInt16;

public class WM {
    private const byte addressSizeInBytes = sizeof(Address) / sizeof(byte);
    private readonly byte[] byteCode;
    private readonly CBVariable[] variables;
    private readonly Stack<ICBValue> stack= new(200); // this much should be enough to start with

    // when returning form a function, go to the previous location
    // this is where those are stored
    // I am probably doing this wrong but as long as it works...
    private readonly Stack<uint> gotoStack= new(20); // this much should pe plenty as long as someone doesn't go nuts with the language

    private uint programCounter = 0;

    public void Evaluate() {
        if (byteCode.Length == 0)
            return;

        for (; ; ) {
            switch ((OpCode)Read()) {
                case OpCode.Halt:
                    return;
                case OpCode.Push:
                    Push();
                    break;
                case OpCode.Pop:
                    Pop();
                    break;
                case OpCode.Goto:
                    Goto(true);
                    break;
                case OpCode.GotoNoStackPush:
                    Goto(false);
                    break;
                case OpCode.GotoIf:
                    if (ReadBool())
                        Goto(false);
                    break;
                case OpCode.GotoIfNot:
                    if (!ReadBool())
                        Goto(false);
                    break;
                case OpCode.GoBack or OpCode.GotoAfterLoop:
                    Return();
                    break;
                case OpCode.Assign:
                    variables[ReadAddress()].SetValue(variables[ReadAddress()].GetValueUnsafe());
                    break;
                case OpCode.BinaryOperator:
                    ICBValue b = variables[ReadAddress()].GetValueUnsafe();
                    ICBValue a = variables[ReadAddress()].GetValueUnsafe();
                    // FIXME, this could crash with a divide by zero exception
                    PushV(a.ExecuteBinaryOperatorUnsafe(ReadT<TokenType>(), b));
                    break;
                case OpCode.CallNative:
                    // push arg1
                    // ...
                    // push argn
                    // push n
                    // push function
                    CBFunction function = ((CBFunctionValue)variables[ReadAddress()].GetValueUnsafe()).value;
                    uint ArgsLength = ReadT<uint>(); // exist for ...params
                    ICBValue[] arguments = new ICBValue[ArgsLength];
                    for (int i = 0; i < ArgsLength; i++)
                        arguments[i] = variables[ReadAddress()].GetValueUnsafe();
                        // no need for call unsafe because native functions will do a checks anyways
                        // TODO: it might be worth to implement for declared functions
                    PushV(function.Call().Swap().ValueUnsafe());
                    break;

                default:
                    throw new InvalidOperationException($"unknown instruction ({byteCode[programCounter]})");
            }
        }
    }


    // private int ReadInt() {
    //     byte[] bytes= new byte[4];
    //     for (int i = 0; i < 4; i++)
    //         bytes[i] = Read();

    //     if (BitConverter.IsLittleEndian)
    //         Array.Reverse(bytes);
    //     return BitConverter.ToInt32(bytes, 0);
    // }

    // this is me having fun
    private Address ReadAddress() {
        Span<byte> bytes= stackalloc byte[addressSizeInBytes];
        for (int i = 0; i < addressSizeInBytes; i++)
            bytes[i] = Read();

        if (BitConverter.IsLittleEndian)
            bytes.Reverse();
        return MemoryMarshal.Read<Address>(bytes);
    }

    private bool ReadBool() => MemoryMarshal.Read<bool>([Read()]);

    private byte Read() {
        return byteCode[programCounter++];
    }

    private T ReadT<T>() where T : struct {
        int size = Marshal.SizeOf(default(T)) / sizeof(byte);
        Span<byte> bytes = stackalloc byte[size];

        for (int i = 0; i < size; i++)
            bytes[i] = Read();

        if (BitConverter.IsLittleEndian)
            bytes.Reverse();

        return MemoryMarshal.Read<T>(bytes);
    }

    // current op code is push so the next one should be the variable
    private void Push() {
        stack.Push(variables[ReadAddress()].GetValueUnsafe());
    }

    private void PushV(ICBValue value) {
        stack.Push(value);
    }

    private ICBValue Pop() => stack.Pop();

    private void Goto(bool pushStack) {
        if (pushStack)
            gotoStack.Push(programCounter);
        programCounter = ReadAddress();
    }

    private void Return() {
        programCounter = gotoStack.Pop();
    }
}