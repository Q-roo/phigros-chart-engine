using System;
using System.Text;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public static class UnsafeByteCodeGeneratorExtensions {
    public static string Dump(this UnsafeByteCodeGenerator generator) {
        StringBuilder builder = new(200);
        byte[] code = generator.GetCode();
        int i = 0;

        byte Read() {
            return code[i++];
        }

        byte[] ReadN(int size) {
            byte[] bytes = new byte[size];
            for (int i = 0; i < size; i++)
                bytes[i] = Read();
            return bytes;
        }

        Address ReadAddress() => BitConverter.ToUInt16(ReadN(sizeof(Address)));
        int ReadI32() => BitConverter.ToInt32(ReadN(sizeof(int)));
        double ReadF32() => BitConverter.ToDouble(ReadN(sizeof(double)));
        bool ReadBool() => BitConverter.ToBoolean(ReadN(sizeof(bool)));

        while (i < code.Length)
            switch ((UnsafeOpCode)Read()) {
                case UnsafeOpCode.HLT:
                    builder.AppendLine("HLT");
                    break;
                case UnsafeOpCode.NOOP:
                    builder.AppendLine("NOOP");
                    break;
                case UnsafeOpCode.DCLV:
                    builder.Append("DCLV");
                    builder.AppendLine($", {generator.chunkInfo.GetVariableName(ReadAddress())}");
                    break;
                case UnsafeOpCode.ASGN:
                    builder.AppendLine("ASGN");
                    break;
                case UnsafeOpCode.DSPA:
                    builder.Append("DSPA");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.DSPI:
                    builder.Append("DSPI");
                    builder.AppendLine($", {ReadI32()}");
                    break;
                case UnsafeOpCode.DSPD:
                    builder.Append("DSPD");
                    builder.AppendLine($", {ReadF32()}");
                    break;
                case UnsafeOpCode.DSPB:
                    builder.Append("DSPB");
                    builder.AppendLine($", {ReadBool()}");
                    break;
                case UnsafeOpCode.DSPN:
                    builder.AppendLine("DSPN");
                    break;
                case UnsafeOpCode.LCST: {
                    Address address = ReadAddress();
                    builder.Append("LCST");
                    builder.AppendLine($", {address} ({generator.chunkInfo.GetConstant(address)})");
                    break;
                }
                case UnsafeOpCode.ACOL:
                    builder.Append("ACOL");
                    builder.AppendLine($", {ReadI32()}");
                    break;
                case UnsafeOpCode.TRAN:
                    builder.AppendLine("TRAN");
                    break;
                case UnsafeOpCode.TRANI:
                    builder.AppendLine("TRANI");
                    break;
                case UnsafeOpCode.BINOP:
                    builder.Append("BINOP");
                    builder.AppendLine($", {((TokenType)Read()).ToSourceString()}");
                    break;
                case UnsafeOpCode.PREOP:
                    builder.Append("PREOP");
                    builder.AppendLine($", {((TokenType)Read()).ToSourceString()}");
                    break;
                case UnsafeOpCode.POSOP:
                    builder.Append("POSOP");
                    builder.AppendLine($", {((TokenType)Read()).ToSourceString()}");
                    break;
                case UnsafeOpCode.CALL:
                    builder.Append("CALL");
                    builder.AppendLine($", {ReadI32()}");
                    break;
                case UnsafeOpCode.CALLN:
                    builder.Append("CALLN");
                    builder.AppendLine($", {ReadI32()}");
                    break;
                case UnsafeOpCode.LDV:
                    builder.Append("LDV");
                    builder.AppendLine($", {generator.chunkInfo.GetVariableName(ReadAddress())}");
                    break;
                case UnsafeOpCode.LDC: {
                    Address address = ReadAddress();
                    builder.AppendLine($"captures ({string.Join(", ", generator.chunkInfo.GetClosureCaptures(address).Map(generator.chunkInfo.GetVariableName))})");
                    builder.Append("LDC");
                    builder.AppendLine($", {address}");
                    break;
                }
                case UnsafeOpCode.MGET:
                    builder.AppendLine("MGET");
                    break;
                case UnsafeOpCode.JMP:
                    builder.AppendLine("JMP");
                    break;
                case UnsafeOpCode.JMPI:
                    builder.AppendLine("JMPI");
                    break;
                case UnsafeOpCode.JMPN:
                    builder.AppendLine("JMPN");
                    break;
                case UnsafeOpCode.JMPS:
                    builder.AppendLine("JMPS");
                    break;
                case UnsafeOpCode.JMPE:
                    builder.AppendLine("JMPE");
                    break;
                case UnsafeOpCode.JMPNE:
                    builder.AppendLine("JMPNE");
                    break;
                case UnsafeOpCode.ITER:
                    builder.AppendLine("ITER");
                    break;
                case UnsafeOpCode.ITERN:
                    builder.AppendLine("ITERN");
                    break;
                case UnsafeOpCode.LSTART:
                    builder.AppendLine("LSTART");
                    break;
                case UnsafeOpCode.LEND:
                    builder.AppendLine("LEND");
                    break;
                default:
                    builder.AppendLine("unknown");
                    break;
            }

        return builder.ToString();
    }
}