using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LanguageExt.UnsafeValueAccess;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public class VM(byte[] byteCode, CBVariable[] variables) {
    private const byte addressSizeInBytes = sizeof(Address) / sizeof(byte);
    private readonly byte[] byteCode = byteCode;
    private readonly CBVariable[] variables = variables;
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
                case OpCode.DirectPushI32:
                    PushV(new I32Value(ReadT<int>()));
                    break;
                case OpCode.Pop:
                    Pop();
                    break;
                case OpCode.Goto:
                    Goto(true);
                    break;
                case OpCode.GotoRelative:
                    GotoRelative(true);
                    break;
                case OpCode.GotoNoStackPush:
                    Goto(false);
                    break;
                case OpCode.GotoRelativeNoStackPush:
                    GotoRelative(false);
                    break;
                case OpCode.GotoIf:
                    if ((BoolValue)stack.Pop())
                        Goto(false);
                    break;
                case OpCode.GotoIfNot:
                    if (!(BoolValue)stack.Pop())
                        Goto(false);
                    break;
                case OpCode.GotoRelativeIfNot:
                    if (!(BoolValue)stack.Pop())
                        GotoRelative(false);
                    break;
                case OpCode.GotoBack /* or OpCode.GotoAfterLoop */:
                    Return();
                    break;
                case OpCode.Assign:
                    Pop().SetValue(Pop());
                    break;
                case OpCode.BinaryOperator:
                    ICBValue b = Pop();
                    ICBValue a = Pop();
                    // FIXME, this could crash with a divide by zero exception
                    PushV(a.ExecuteBinaryOperatorUnsafe(ReadT<TokenType>(), b));
                    break;
                case OpCode.CallNative:
                    Call(true);
                    break;
                case OpCode.Call:
                    Call(false);
                    break;
                default:
                    throw new InvalidOperationException($"unknown instruction ({byteCode[programCounter]})");
            }
        }
    }

    public string Dump() {
        StringBuilder builder = new(300);

        for (; programCounter < byteCode.Length;) {
            switch ((OpCode)Read()) {
                case OpCode.Halt:
                    builder.AppendLine("HLT");
                    break;
                case OpCode.Pop:
                    builder.AppendLine("POP");
                    break;
                case OpCode.Push:
                    builder.Append("PUSH");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.DirectPushI32:
                    builder.Append("DPI");
                    builder.AppendLine($", {ReadT<int>()}");
                    break;
                case OpCode.Goto:
                    builder.Append("JMP");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoIf:
                    builder.Append("JMPI");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoIfNot:
                    builder.Append("JMPE");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoNoStackPush:
                    builder.Append("JMPN");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoBack:
                    builder.AppendLine("RET");
                    break;
                case OpCode.GotoRelative:
                    builder.Append("JMPR");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoRelativeIf:
                    builder.Append("JMPRI");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoRelativeIfNot:
                    builder.Append("JMPRE");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.GotoRelativeNoStackPush:
                    builder.Append("JMPRN");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.IterNextOrGotoRelative:
                    builder.Append("ITNOJMPR");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case OpCode.Assign:
                    builder.AppendLine("ASGN");
                    break;
                case OpCode.BinaryOperator:
                    builder.Append("BINOP");
                    builder.AppendLine($", {ReadT<TokenType>()}");
                    break;
                case OpCode.Call: {
                    builder.Append("CALL");
                    builder.Append($", {ReadAddress()}");
                    uint argCount = ReadT<uint>();
                    builder.Append($", {argCount}");
                    for (int i = 0; i < argCount; i++)
                        builder.Append($", {ReadAddress()}");

                    builder.AppendLine();
                    break;
                }
                case OpCode.CallNative: {
                    builder.Append("CALLN");
                    uint argCount = ReadT<uint>();
                    builder.Append($", {argCount}");
                    for (int i = 0; i < argCount; i++)
                        builder.Append($", {ReadAddress()}");

                    builder.AppendLine();
                    break;
                }
                default:
                    builder.AppendLine("unknown instruction");
                    break;
            }
        }

        return builder.ToString();
    }

    // this is me having fun
    private Address ReadAddress() {
        Span<byte> bytes= stackalloc byte[addressSizeInBytes];
        for (int i = 0; i < addressSizeInBytes; i++)
            bytes[i] = Read();

        if (BitConverter.IsLittleEndian)
            bytes.Reverse();

        return MemoryMarshal.Read<Address>(bytes);
    }

    private bool ReadBool() => BitConverter.ToBoolean([Read()]);

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

    private void GotoRelative(bool pushStack) {
        if (pushStack)
            gotoStack.Push(programCounter);

        programCounter = (uint)(programCounter + ReadT<int>());
    }

    private void Return() {
        programCounter = gotoStack.Pop();
    }

    private void Call(bool isNative) {
        // push arg1
        // ...
        // push argn
        // push n
        // push function

        Address functionLocation = 0;
        CBFunction function = null;

        if (isNative)
            function = ((CBFunctionValue)Pop()).value;
        else
            functionLocation = ReadAddress();

        uint ArgsLength = ReadT<uint>(); // exist for ...params
        ICBValue[] arguments = new ICBValue[ArgsLength];

        for (int i = 0; i < ArgsLength; i++)
            arguments[i] = variables[ReadAddress()].GetValueUnsafe();

        if (isNative)
            // no need for call unsafe because native functions will do a lot of checks anyways
            PushV(function.Call(arguments).Swap().ValueUnsafe());
        else {
            gotoStack.Push(programCounter);
            foreach (ICBValue arg in arguments)
                stack.Push(arg);

            programCounter = functionLocation;
        }
    }
}