using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public class UnsafeByteCodeGenerator {
    // functions are top level
    // functions inside functions are handled as if they were closure

    class Chunk {
        public List<byte> code = new(200);

        private readonly List<CBVariable> variables = [];
        private readonly Dictionary<string, CBVariable> variablesWithNames = [];
        private readonly Dictionary<CBVariable, Address> variableAddressLookup = [];

        private readonly List<object> constantPool = [];
        private  readonly Dictionary<object, Address> constantAddressLookup = [];

        public Address DeclareVariable(string name, CBVariable variable) {
            if (!HasConstant(name))
                AddConstant(name);

            // TODO: check if exists
            variables.Add(variable);
            variablesWithNames[name] = variable;
            Address address = (Address)(variables.Count - 1);
            variableAddressLookup[variable] = address;
            return address;
        }

        public Address Lookup(string name) {
            return variableAddressLookup[variablesWithNames[name]];
        }

        public Address Lookup(CBVariable variable) {
            return variableAddressLookup[variable];
        }

        public Address AddConstant(object constant) {
            constantPool.Add(constant);
            Address address = (Address)(constantPool.Count - 1);
            constantAddressLookup[constant] = address;
            return address;
        }

        public bool HasConstant(object constant) {
            return constantAddressLookup.ContainsKey(constant);
        }

        public Address ConstantLookup(object constant) {
            return constantAddressLookup[constant];
        }
    }

    private readonly List<Chunk> chunks = new(200);
    public byte[] GetCode() => chunks.SelectMany(it => it.code).ToArray();

    public string Dump() {
        StringBuilder builder = new(200);
        byte[] code = GetCode();
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
                case UnsafeOpCode.DCLV:
                    builder.Append("DCLV");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.ASGN:
                    builder.AppendLine("ASGN");
                    break;
                case UnsafeOpCode.FRO:
                    builder.Append("FRO");
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
                case UnsafeOpCode.LCST:
                    builder.Append("LCST");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
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
                case UnsafeOpCode.IGET:
                    builder.Append("IGET");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.MGET:
                    builder.AppendLine("MGET");
                    break;
                case UnsafeOpCode.JMP:
                    builder.Append("JMP");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.JMPI:
                    builder.Append("JMPI");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.JMPN:
                    builder.Append("JMPN");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.APUSH:
                    builder.Append("APUSH");
                    builder.AppendLine($", {ReadAddress()}");
                    break;
                case UnsafeOpCode.APOP:
                    builder.AppendLine("APOP");
                    break;
                case UnsafeOpCode.ITER:
                    builder.AppendLine("ITER");
                    break;
                default:
                    builder.AppendLine("unknown");
                    break;
            }

        return builder.ToString();
    }

    public UnsafeByteCodeGenerator Generate(ASTRoot ast) {
        GenerateChunk(ast);
        chunks.Add(new() { code = [UnsafeOpCode.HLT.AsByte()] });
        return this;
    }


    private void GenerateChunk(BlockStatementNode block) {
        Chunk chunk = new();
        foreach (StatementNode statement in block.body)
            GenerateStatement(statement, chunk);

        chunks.Add(chunk);
    }

    private void GenerateStatement(StatementNode statement, Chunk chunk) {
        switch (statement) {
            case EmptyStatementNode:
            case CommandStatementNode: // this shouldn't be here
                break;
            case BlockStatementNode block:
                GenerateChunk(block);
                break;
            case ExpressionStatementNode expression:
                GenerateExpression(expression.expression, chunk);
                break;
            case VariableDeclarationStatementNode variableDeclaration: {
                Address address = chunk.DeclareVariable(variableDeclaration.name, new(variableDeclaration.@readonly));
                // TODO: maybe the address a and the identifier address should be related
                chunk.code.Add(UnsafeOpCode.DCLV.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(address));

                if (variableDeclaration.@readonly) {
                    chunk.code.Add(UnsafeOpCode.FRO.AsByte());
                    chunk.code.AddRange(BitConverter.GetBytes(address));
                }


                if (!variableDeclaration.valueExpression.IsNullOrEmpty()) {
                    GenerateExpression(variableDeclaration.valueExpression, chunk);
                    GenerateExpression(new AssignmentExpressionNode(new IdentifierExpressionNode(variableDeclaration.name), new Token(-1, -1, TokenType.Equal), variableDeclaration.valueExpression), chunk);
                }
                break;
            }
            case FunctionDeclarationStatementNode:
            case BreakStatementNode:
            case ContinueStatementNode:
            case ReturnStatementNode:
                throw new NotImplementedException();
            case ForeachLoopStatementNode @foreach: {
                // TODO
                // declare tmp var
                // mkiter
                // store iter in tmp
                // loop start
                // iter next &tmp
                // jump if not address(loop body size)
                // body
                // + jump to start
                // TODO: random name
                Address address = chunk.DeclareVariable("/iter", new(true));
                GenerateExpression(@foreach.iterable, chunk);
                chunk.code.Add(UnsafeOpCode.ITER.AsByte());
                throw new NotImplementedException();
            }
            case ForLoopStatementNode @for: {
                // a for loop can be turned into a while loop without much trouble
                if (!@for.init.IsNullOrEmpty())
                    GenerateStatement(@for.init, chunk);

                BlockStatementNode body = @for.body is BlockStatementNode block ? block : new([]);
                if (!@for.update.IsNullOrEmpty())
                    body.body.Add(new ExpressionStatementNode(@for.update));

                WhileLoopStatementNode @while = new(@for.condition ?? new IdentifierExpressionNode("true"), body);
                GenerateStatement(@while, chunk);
                break;
            }
            case WhileLoopStatementNode @while: {
                // loop start
                // condition
                // jump if not
                // address
                // body
                // jump to start
                // TODO: not 2 but the size of a the opcode + args
                Address start = (Address)chunk.code.Count;
                Chunk tmp = new();

                GenerateStatement(@while.body, tmp);
                tmp.code.Add(UnsafeOpCode.JMP.AsByte());
                tmp.code.AddRange(BitConverter.GetBytes(start));

                GenerateExpression(@while.condition, chunk);
                chunk.code.Add(UnsafeOpCode.JMPN.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes((Address)tmp.code.Count));
                chunk.code.AddRange(tmp.code);
                break;
            }
            case IfStatementNode @if: {
                // the ternary operator was implemented before the if
                // but now, if handles both
                // true ? 1 : 0
                // 16 bytes
                // DSPB (1), True (1)
                // JMPI (1), size+opcode size (2) <- might be better to use 10
                // DSPI (1), 0 (4)
                // DSPI (1), 1 (4)
                // HLT (1)
                Chunk tmp = new();
                GenerateStatement(@if.@false, tmp);
                GenerateExpression(@if.condition, chunk);
                chunk.code.Add(UnsafeOpCode.JMPI.AsByte());
                // TODO: not 3 but the size of a the opcode + args
                chunk.code.AddRange(BitConverter.GetBytes((Address)(chunk.code.Count + tmp.code.Count + 3)));
                chunk.code.AddRange(tmp.code);
                GenerateStatement(@if.@true, chunk);
                break;
            }
            default:
                throw new NotImplementedException($"generation not implemented for {statement}");
        }
    }

    private void GenerateExpression(ExpressionNode expression, Chunk chunk) {
        switch (expression) {
            case IntExpressionNode @int:
                chunk.code.Add(UnsafeOpCode.DSPI.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(@int.value));
                break;
            case DoubleExpressionNode @double:
                chunk.code.Add(UnsafeOpCode.DSPD.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(@double.value));
                break;
            case StringExpressionNode @string: {
                string value = @string.value;
                Address address = chunk.HasConstant(value) ? chunk.ConstantLookup(value) : chunk.AddConstant(value);
                chunk.code.Add(UnsafeOpCode.LCST.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(address));
                break;
            }
            case ArrayLiteralExpressionNode array:
                foreach (ExpressionNode item in array.content)
                    GenerateExpression(item, chunk);

                chunk.code.Add(UnsafeOpCode.ACOL.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(array.content.Length));
                break;
            case BinaryExpressionNode binary:
                GenerateExpression(binary.left, chunk);
                GenerateExpression(binary.right, chunk);
                chunk.code.Add(UnsafeOpCode.BINOP.AsByte());
                chunk.code.Add((byte)binary.@operator.Type);
                break;
            case CallExpressionNode call: {
                foreach (ExpressionNode argument in call.arguments)
                    GenerateExpression(argument, chunk);

                GenerateExpression(call.method, chunk);
                chunk.code.Add((call.isNative ? UnsafeOpCode.CALLN : UnsafeOpCode.CALL).AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(call.arguments.Length));
                break;
            }
            case IdentifierExpressionNode identifier: {
                string value = identifier.value;
                switch (value) {
                    case "true":
                        chunk.code.Add(UnsafeOpCode.DSPB.AsByte());
                        chunk.code.AddRange(BitConverter.GetBytes(true));
                        break;
                    case "false":
                        chunk.code.Add(UnsafeOpCode.DSPB.AsByte());
                        chunk.code.AddRange(BitConverter.GetBytes(false));
                        break;
                    case "unset":
                        chunk.code.Add(UnsafeOpCode.DSPN.AsByte());
                        break;
                    default:
                        // TODO: address should exist
                        Address address = chunk.HasConstant(value) ? chunk.ConstantLookup(value) : chunk.AddConstant(value);
                        chunk.code.Add(UnsafeOpCode.IGET.AsByte());
                        chunk.code.AddRange(BitConverter.GetBytes(address));
                        break;
                }
                break;
            }
            case MemberAccessExpressionNode memberAccess:
                GenerateExpression(new ComputedMemberAccessExpressionNode(memberAccess.member, new StringExpressionNode(memberAccess.property)), chunk);
                break;
            case ComputedMemberAccessExpressionNode computedMemberAccess:
                GenerateExpression(computedMemberAccess.member, chunk);
                GenerateExpression(computedMemberAccess.property, chunk);
                chunk.code.Add(UnsafeOpCode.MGET.AsByte());
                break;
            case TernaryExpressionNode ternary:
                // this should have the same effect
                // since the result of the expression
                // gets pushed to the stack anyways
                GenerateStatement(new IfStatementNode(
                    ternary.condition,
                    new ExpressionStatementNode(ternary.@true),
                    new ExpressionStatementNode(ternary.@false)
                ), chunk);
                break;
            case AssignmentExpressionNode assignment: {
                TokenType @operator = assignment.@operator.Type;
                // .= is a = a.method()
                if (assignment.value is not CallExpressionNode call)
                    throw new Exception();

                if (@operator == TokenType.DotAssign) {
                    GenerateExpression(
                        new AssignmentExpressionNode(
                            assignment.asignee,
                            new Token(-1, -1, TokenType.Assign),
                            new CallExpressionNode(new ComputedMemberAccessExpressionNode(assignment.asignee, call.method), call.arguments)
                        ),
                        chunk
                    );
                    break;
                }
                if (@operator != TokenType.Assign)
                    assignment.value = new BinaryExpressionNode(assignment.asignee, new Token(-1, -1, @operator switch {
                        TokenType.PlusAssign => TokenType.Plus,
                        TokenType.MinusAssign => TokenType.Minus,
                        TokenType.MultiplyAssign => TokenType.Multiply,
                        TokenType.PowerAssign => TokenType.Power,
                        TokenType.DivideAssign => TokenType.Divide,
                        TokenType.ModuloAssign => TokenType.Modulo,
                        TokenType.ShiftLeftAssign => TokenType.ShiftLeft,
                        TokenType.ShiftRightAssign => TokenType.ShiftRight,
                        TokenType.BitwiseAndAssign => TokenType.BitwiseAnd,
                        TokenType.BitwiseOrAssign => TokenType.BitwiseOr,
                        TokenType.BitwiseXorAssign => TokenType.BitwiseXor,
                        TokenType.BitwiseNotAssign => TokenType.BitwiseNot,
                        _ => throw new UnreachableException()
                    }), assignment.value);

                GenerateExpression(assignment.asignee, chunk);
                GenerateExpression(assignment.value, chunk);
                chunk.code.Add(UnsafeOpCode.ASGN.AsByte());
                break;
            }
            case ClosureExpressionNode closure:
                throw new NotImplementedException();
            case EmptyExpressionNode:
                break;
            // probably not the correct use but it makes this simpler
            case UnaryExpressionNode unary:
                GenerateExpression(unary.expression, chunk);
                chunk.code.Add((unary.prefix ? UnsafeOpCode.PREOP : UnsafeOpCode.POSOP).AsByte());
                chunk.code.Add((byte)unary.@operator.Type);
                break;
            case RangeLiteralExpressionNode range:
                GenerateExpression(range.start, chunk);
                GenerateExpression(range.end, chunk);
                chunk.code.Add((range.inclusiveEnd ? UnsafeOpCode.TRANI : UnsafeOpCode.TRAN).AsByte());
                break;
            default:
                throw new NotImplementedException($"generation not implemented for {expression}");
        }
    }
}
