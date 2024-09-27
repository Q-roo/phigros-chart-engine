using System;
using System.Diagnostics;
using System.Text;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public class UnsafeByteCodeGenerator {
    // functions are top level
    // functions inside functions are handled as if they were closure
    // all functions are handled as if they were closures

    private readonly ChunkInfo chunkInfo = new();
    private ByteCodeChunk RootChunk;
    public byte[] GetCode() {
        return [.. RootChunk.code];
    }

    public UnsafeVM BuildVM() => new(RootChunk);

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
                case UnsafeOpCode.NOOP:
                    builder.AppendLine("NOOP");
                    break;
                case UnsafeOpCode.DCLV:
                    builder.Append("DCLV");
                    builder.AppendLine($", {chunkInfo.GetVariableName(ReadAddress())}");
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
                case UnsafeOpCode.SPOP:
                    builder.AppendLine("SPOP");
                    break;
                case UnsafeOpCode.LCST:
                    Address address = ReadAddress();
                    builder.Append("LCST");
                    builder.AppendLine($", {address} ({chunkInfo.GetConstant(address)})");
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
                    case UnsafeOpCode.LDV:
                    builder.Append("LDV");
                    builder.AppendLine($", {chunkInfo.GetVariableName(ReadAddress())}");
                    break;
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
                case UnsafeOpCode.RET:
                    builder.AppendLine("RET");
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
                case UnsafeOpCode.CPTR:
                    builder.AppendLine("CPTR");
                    break;
                case UnsafeOpCode.CSTART:
                    builder.AppendLine("CSTART");
                    break;
                case UnsafeOpCode.CEND:
                    builder.AppendLine("CEND");
                    break;
                default:
                    builder.AppendLine("unknown");
                    break;
            }

        return builder.ToString();
    }

    public UnsafeByteCodeGenerator Generate(ASTRoot ast) {
        SetupChunkInfo();
        GenerateRootChunk();
        GenerateChunk(ast, RootChunk);
        RootChunk.ResloveLoopLabels();
        RootChunk.code.Add(UnsafeOpCode.HLT.AsByte());
        return this;
    }

    private void SetupChunkInfo() {
        // TODO: functions like print()
    }

    private ByteCodeChunk CreateChunk(ByteCodeChunk parent) => new(parent, false, chunkInfo);
    private ByteCodeChunk CreateTemporaryChunk(ByteCodeChunk parent) => new(parent, true, chunkInfo);

    private void GenerateRootChunk() {
        RootChunk = CreateChunk(null);
    }

    private void GenerateChunk(BlockStatementNode block, ByteCodeChunk parent) {
        ByteCodeChunk chunk = CreateChunk(parent);
        foreach (StatementNode statement in block.body)
            GenerateStatement(statement, chunk);

        parent.Merge(chunk);
    }

    // handle all functions as if they were closures
    private void GenerateFunctionChunk(ClosureExpressionNode closure, ByteCodeChunk parent) {
        ByteCodeChunk chunk = CreateChunk(parent);

        chunk.code.Add(UnsafeOpCode.CPTR.AsByte());
        foreach (FunctionParameter argument in closure.arguments) {
            chunk.DeclareVariable(argument.name, new());
            // Address address = chunkInfo.AddOrGetConstant(argument.name);
            // chunk.code.Add(UnsafeOpCode.IGET.AsByte());
            Address address = chunk.DeclareOrGet(argument.name, new());
            chunk.code.Add(UnsafeOpCode.LDV.AsByte());
            chunk.code.AddRange(BitConverter.GetBytes(address));
            chunk.code.Add(UnsafeOpCode.SPOP.AsByte());
            chunk.code.Add(UnsafeOpCode.ASGN.AsByte());
        }

        GenerateStatement(closure.body, chunk);

        // FIXME: this could result in 2 RET instructions, it won't cause bugs but it does increase the size with 1 byte
        chunk.code.Add(UnsafeOpCode.RET.AsByte());

        parent.Merge(chunk);
    }

    private void GenerateStatement(StatementNode statement, ByteCodeChunk chunk) {

        switch (statement) {
            case null: // some statements can have null as a body
            case EmptyStatementNode:
            case CommandStatementNode: // this shouldn't be here
                break;
            case BlockStatementNode block:
                GenerateChunk(block, chunk);
                break;
            case ExpressionStatementNode expression:
                GenerateExpression(expression.expression, chunk);
                break;
            case VariableDeclarationStatementNode variableDeclaration: {
                Address address = chunk.DeclareVariable(variableDeclaration.name, new());
                // TODO: maybe the address and the identifier address should be related
                chunk.code.Add(UnsafeOpCode.DCLV.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes(address));

                if (!variableDeclaration.valueExpression.IsNullOrEmpty())
                    GenerateExpression(new AssignmentExpressionNode(new IdentifierExpressionNode(variableDeclaration.name), new Token(-1, -1, TokenType.Assign), variableDeclaration.valueExpression), chunk);

                break;
            }
            case FunctionDeclarationStatementNode functionDeclaration: {
                // declare a variable in the chunk with the name of the function
                GenerateStatement(new VariableDeclarationStatementNode(true, functionDeclaration.name, null, null), chunk);
                // load the address of the variable onto the stack
                GenerateExpression(new IdentifierExpressionNode(functionDeclaration.name), chunk);
                // construct the closure which will be on the stack
                GenerateExpression(new ClosureExpressionNode(functionDeclaration.arguments, functionDeclaration.returnType, functionDeclaration.body, functionDeclaration.isLastParams), chunk);
                // assign the closure to the variable
                chunk.code.Add(UnsafeOpCode.ASGN.AsByte());
                break;
            }
            case BreakStatementNode:
                chunk.code.Add(UnsafeOpCode.JMPE.AsByte());
                // will be replaced with
                // dspa, address
                // jmp
                for (int i = 0; i < UnsafeOpCode.DSPA.SizeOf(); i++)
                    chunk.code.Add(UnsafeOpCode.NOOP.AsByte()); // jmpe already takes care of the space needed for jmp

                break;
            case ContinueStatementNode:
                chunk.code.Add(UnsafeOpCode.JMPS.AsByte());
                // will be replaced with
                // dspa, address
                // jmp
                for (int i = 0; i < UnsafeOpCode.DSPA.SizeOf(); i++)
                    chunk.code.Add(UnsafeOpCode.NOOP.AsByte()); // jmps already takes care of the space needed for jmp

                break;
            case ReturnStatementNode @return:
                if (@return.value is not null)
                    GenerateExpression(@return.value, chunk);

                chunk.code.Add(UnsafeOpCode.RET.AsByte());
                break;
            case ForeachLoopStatementNode @foreach: {
                // store the iterable's iterator in a variable
                // TODO: random name
                chunk.DeclareVariable("/iter", new());
                // get the variable noto the stack
                GenerateExpression(new IdentifierExpressionNode("/iter"), chunk);
                // generate the iterable
                GenerateExpression(@foreach.iterable, chunk);
                // get the iterator from the iterable
                chunk.code.Add(UnsafeOpCode.ITER.AsByte());
                // assign it to the variable
                chunk.code.Add(UnsafeOpCode.ASGN.AsByte());

                // start of the loop
                chunk.code.Add(UnsafeOpCode.LSTART.AsByte());
                // get the iterator to the stack
                GenerateExpression(new IdentifierExpressionNode("/iter"), chunk);
                // get the next element
                chunk.code.Add(UnsafeOpCode.ITERN.AsByte());
                // jump to the end of the loop if the get next was unsuccessful (the iterator is consumed)
                chunk.code.Add(UnsafeOpCode.JMPNE.AsByte());
                // will be replaced with
                // dspa, address
                // jmpn
                for (int i = 0; i < UnsafeOpCode.DSPA.SizeOf(); i++)
                    chunk.code.Add(UnsafeOpCode.NOOP.AsByte()); // jmpne already takes care of the space needed for jmpn

                // loop body
                GenerateStatement(@foreach.body, chunk);
                // go to the start of the loop
                chunk.code.Add(UnsafeOpCode.JMPS.AsByte());
                // will be replaced with
                // dspa, address
                // jmp
                for (int i = 0; i < UnsafeOpCode.DSPA.SizeOf(); i++)
                    chunk.code.Add(UnsafeOpCode.NOOP.AsByte()); // jmps already takes care of the space needed for jmp

                // end of the loop
                chunk.code.Add(UnsafeOpCode.LEND.AsByte());
                break;
            }
            case ForLoopStatementNode @for: {
                // a for loop can be turned into a while loop without much trouble
                if (!@for.init.IsNullOrEmpty())
                    GenerateStatement(@for.init, chunk);

                BlockStatementNode body = @for.body is BlockStatementNode block ? block : new([@for.body]);
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

                chunk.code.Add(UnsafeOpCode.LSTART.AsByte());
                GenerateExpression(@while.condition, chunk);
                chunk.code.Add(UnsafeOpCode.JMPNE.AsByte());
                // will be replaced with
                // dspa, address
                // jmpn
                for (int i = 0; i < UnsafeOpCode.DSPA.SizeOf(); i++)
                    chunk.code.Add(UnsafeOpCode.NOOP.AsByte()); // jmpne already takes care of the space needed for jmpn

                GenerateStatement(@while.body, chunk);
                chunk.code.Add(UnsafeOpCode.JMPS.AsByte());
                // will be replaced with
                // dspa, address
                // jmp
                for (int i = 0; i < UnsafeOpCode.DSPA.SizeOf(); i++)
                    chunk.code.Add(UnsafeOpCode.NOOP.AsByte()); // jmps already takes care of the space needed for jmp

                chunk.code.Add(UnsafeOpCode.LEND.AsByte());
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
                // ByteCodeChunk tmp = CreateTemporaryChunk(chunk);
                // GenerateStatement(@if.@false, tmp);
                // GenerateExpression(@if.condition, chunk);
                // chunk.code.Add(UnsafeOpCode.DSPA.AsByte());
                // // take the size of the address and the next instruction into consideration during the calculations
                // chunk.code.AddRange(BitConverter.GetBytes((Address)(chunk.code.Count + tmp.code.Count + UnsafeOpCode.JMPI.SizeOf() + sizeof(Address))));
                // chunk.code.Add(UnsafeOpCode.JMPI.AsByte());
                // chunk.MergeTemporary(tmp);
                // GenerateStatement(@if.@true, chunk);
                ByteCodeChunk @true = CreateTemporaryChunk(chunk);
                ByteCodeChunk @false = CreateTemporaryChunk(chunk);

                GenerateStatement(@if.@true, @true);
                GenerateStatement(@if.@false, @false);

                GenerateExpression(@if.condition, chunk);

                // calculate the size of the cureent and the next instruction as well

                // test: [0 ? 2 : 3];
                /*            size    total
                --- condition
                DSPI, 0     ; 5     ; 5
                --- jump to the true branch
                DSPA, 18    ; 3     ; 8
                JMPI        ; 1     ; 9
                --- false branch
                DSPI, 3     ; 5     ; 14
                DSPA, 20    ; 3     ; 17
                JMP         ; 1     ; 18
                --- true branch
                DSPI, 2     ; 5     ; 23
                --- rest
                ACOL, 1     ; 5     ; 28
                HLT         ; 1     ; 29
                */

                // FIXME: wrong jump targets
                @false.code.Add(UnsafeOpCode.DSPA.AsByte());
                @false.code.AddRange(BitConverter.GetBytes((Address)(
                    // -4 is true
                    chunk.code.Count + @false.code.Count + @true.code.Count - UnsafeOpCode.JMP.SizeOf() - sizeof(Address)
                )));
                @false.code.Add(UnsafeOpCode.JMP.AsByte());

                chunk.code.Add(UnsafeOpCode.DSPA.AsByte());
                chunk.code.AddRange(BitConverter.GetBytes((Address)(chunk.code.Count + @false.code.Count + UnsafeOpCode.JMPI.SizeOf() + sizeof(Address))));
                chunk.code.Add(UnsafeOpCode.JMPI.AsByte());
                chunk.MergeTemporary(@false);
                chunk.MergeTemporary(@true);
                break;
            }
            default:
                throw new NotImplementedException($"generation not implemented for {statement.GetType()}");
        }
    }

    private void GenerateExpression(ExpressionNode expression, ByteCodeChunk chunk) {
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
                Address address = chunkInfo.AddOrGetConstant(value);
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
                        // Address address = chunkInfo.AddOrGetConstant(value); // the address should exist
                        // chunk.code.Add(UnsafeOpCode.IGET.AsByte());
                        Address address = chunk.Lookup(identifier.value);
                        chunk.code.Add(UnsafeOpCode.LDV.AsByte());
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

                if (@operator == TokenType.DotAssign) {
                    // .= is a = a.method()
                    if (assignment.value is not CallExpressionNode call)
                        throw new Exception();

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
                chunk.code.Add(UnsafeOpCode.CSTART.AsByte());
                // the merging happens in the method
                GenerateFunctionChunk(closure, chunk);
                chunk.code.Add(UnsafeOpCode.CEND.AsByte());
                break;
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
