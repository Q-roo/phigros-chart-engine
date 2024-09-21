using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PCE.Chartbuild.Runtime;

using Address = ushort;
using ByteCodeOutput = List<byte>;

public class ByteCodeGenerator(ASTRoot ast) {
    private readonly ASTRoot ast = ast;

    // TODO: find a good start sizes
    // user defined functions go here
    private readonly ByteCodeOutput functionCode = new(1000);
    private readonly ByteCodeOutput code = new(2000);
    private readonly List<CBVariable> variables = new(200);
    private readonly Dictionary<CBVariable, Address> variableAddressLookup = new(200);
    private readonly Dictionary<StatementNode, Address> gotoIndexLookup = new(200);
    public ushort dataLocationOffset;

    // TODO:
    // since the wm reads the code byte by byte,
    // a function declaration in the middle of the code would result in...chaos
    // so function body code should be either at the start or at the end
    // with an offset to make sure the addresses are correct

    public VM CreateVM() => new(Generate(), [.. variables]);

    public byte[] Generate() {
        //gotoIndexLookup.Add(ast, 0);
        // probably useless
        GenerateBlock(ast, code, false, false, null, null);

        dataLocationOffset = (ushort)(code.Count - 1);
        return [.. code, (byte)OpCode.Halt, .. functionCode];
    }

    private static Func<int> CreateCurrentLoopSizeGetter(ByteCodeOutput output) => () => output.Count;
    private void AddVariableToLookup(CBVariable variable) {
        if (variableAddressLookup.ContainsKey(variable))
        return;

        variables.Add(variable);
        variableAddressLookup[variable] = (Address)Godot.Mathf.Max(variables.Count - 1, 0); // the first addres would be -1 (which then would wrap) otherwise
    }

    private void GenerateExpression(ExpressionNode expression, ByteCodeOutput output) {
        switch (expression) {
            case AssignmentExpressionNode assignment:
                GenerateAssignment(assignment, output);
                break;
            case BinaryExpressionNode binary:
                GenerateBinaryOperation(binary, output);
                break;
            case CallExpressionNode call:
                GenerateCall(call, output);
                break;
            case ClosureExpressionNode:
                // TODO: still not implemented
                throw new NotImplementedException("TODO: closures");
            case ComputedMemberAccessExpressionNode computedMemberAccess:

            case MemberAccessExpressionNode:
            case IdentifierExpressionNode:
            case PrefixExpressionNode:
            case PostfixExpressionNode:
            case TernaryExpressionNode:
            default:
                break;
        }
    }

    private void GenerateAssignment(AssignmentExpressionNode assignment, ByteCodeOutput output) {
        // push the asigned value onto the stack
        // call assign

        // push value a onto the stack
        GenerateExpression(assignment.asignee, output);

        // push value b onto the stack

        // cases like +=, ...etc
        // since call is not a binary expression node,
        // it shouldn't be handled by this branch
        if (assignment.@operator.Type != TokenType.Assign && assignment.@operator.Type != TokenType.DotAssign) {
            BinaryExpressionNode binary = new(
                        assignment.asignee,
                        new Token(
                            assignment.@operator.lineNumber,
                            assignment.@operator.columnNumber,
                            assignment.@operator.Type switch {
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
                                TokenType.BitswiseNotAssign => TokenType.BitwiseNot,
                                _ => throw new UnreachableException()
                            }
                        ),
                        assignment.value
                    );

            GenerateBinaryOperation(binary, output);
        } else
            GenerateExpression(assignment.value, output);

        output.Add((byte)OpCode.Assign);
    }

    private void GenerateBinaryOperation(BinaryExpressionNode binary, ByteCodeOutput output) {
        GenerateExpression(binary.left, output);
        GenerateExpression(binary.right, output);
        output.Add((byte)binary.@operator.Type);
        output.Add((byte)OpCode.BinaryOperator);
        // the vm will push the result to the stack
    }

    private void GenerateCall(CallExpressionNode call, ByteCodeOutput output) {
        Address start = (Address)output.Count;

        foreach (ExpressionNode argument in call.arguments)
            GenerateExpression(argument, output);
        
        output.Add((byte)OpCode.DirectPushI32);
        output.AddRange(BitConverter.GetBytes(call.arguments.Length));
        
        if (call.isNative)
        {
            // native functions don't have a bytecode body
            // just push the fucntion onto the stack
            GenerateExpression(call.method, output);
            output.Add((byte)OpCode.CallNative);
        }
        else {
            // user defined functions have a bytecode body
            // put the address of the function body
            output.AddRange(BitConverter.GetBytes(start));
            output.Add((byte)OpCode.CallNative);
        }
    }

    private void GenerateComputedMemberAccess(ComputedMemberAccessExpressionNode computedMemberAccess, ByteCodeOutput output) {

    }

    private void GenerateBlock(BlockStatementNode body, ByteCodeOutput output, bool isInFunctionBody, bool isInLoopBody, Func<int> getCurrentLoopBodySize, List<int> breakPositions) {
        // TODO: might not be neccassary to add this to the lookup
        // gotoIndexLookup[body] = code.Count - 1;
        foreach (CBVariable variable in body.scope.AllVariables) {
            AddVariableToLookup(variable);

            if (variable.GetValueUnsafe() is CBFunctionValue functionValue && functionValue.value is DeclaredFunction function)
                GenerateFunction(function, getCurrentLoopBodySize, breakPositions);
        }

        foreach (StatementNode statement in body.body)
            GenerateStatement(statement, output, isInFunctionBody, isInLoopBody, getCurrentLoopBodySize, breakPositions);
    }

    private void GenerateStatement(StatementNode statement, ByteCodeOutput output, bool isInFunctionBody, bool isInLoopBody, Func<int> getCurrentLoopBodySize, List<int> breakPositions) {
        gotoIndexLookup[statement] = (Address)(code.Count - 1);
        switch (statement) {
            case BlockStatementNode block:
                GenerateBlock(block, output, isInFunctionBody, isInLoopBody, getCurrentLoopBodySize, breakPositions);
                break;
            case BreakStatementNode when isInLoopBody:
                DeferGenerateBreak(output, getCurrentLoopBodySize, breakPositions);
                break;
            case ContinueStatementNode when isInLoopBody:
                GenerateContinue(output, getCurrentLoopBodySize);
                break;
            case ReturnStatementNode @return when isInFunctionBody:
                GenerateReturn(@return, output);
                break;
            case ExpressionStatementNode expression:
                GenerateExpression(expression.expression, output);
                break;
            case ForeachLoopStatementNode @foreach:
                GenerateForEachLoop(@foreach, output, isInFunctionBody);
                break;
            case ForLoopStatementNode @for:
                GenerateForLoop(@for, output, isInFunctionBody);
                break;
            case WhileLoopStatementNode @while:
                GenerateWhileLoop(@while, output, isInFunctionBody);
                break;
            case IfStatementNode @if:
                GenerateIf(@if, output, isInFunctionBody, isInLoopBody, getCurrentLoopBodySize, breakPositions);
                break;
            // statements that should have been removed by the analyzer
            default:
                throw new UnreachableException();
        }
    }

    // since the end of the loop is not known yet,
    // record the positions of the break instructions
    // and insert the locations later
    private void DeferGenerateBreak(ByteCodeOutput output, Func<int> getCurrentLoopBodySize, List<int> breakPositions) {
        output.Add((byte)OpCode.GotoRelativeNoStackPush);
        breakPositions.Add(getCurrentLoopBodySize());
    }

    // continue is the easier one of the two
    // since the start of the loop is known
    // and the current size is also known
    // jumping back is a simple task
    private void GenerateContinue(ByteCodeOutput output, Func<int> getCurrentLoopBodySize) {
        output.Add((byte)OpCode.GotoRelativeNoStackPush);
        output.AddRange(BitConverter.GetBytes(-getCurrentLoopBodySize()));
    }

    private void GenerateReturn(ReturnStatementNode @return, ByteCodeOutput output) {
        if (@return.value is not null)
            GenerateExpression(@return.value, output);

        output.Add((byte)OpCode.GotoBack);
    }

    private void GenerateFunction(DeclaredFunction function, Func<int> getCurrentLoopBodySize, List<int> breakPositions) {
        // the wm will take care of the function arguments
        // they are right on the stack after all
        ByteCodeOutput body = new(200);
        // the following shouldn't be allowed
        // while(true)
        // const a = () => {break;}
        // so set isInLoopBody to false
        GenerateBlock(function.body, body, true, false, getCurrentLoopBodySize, breakPositions);
        functionCode.AddRange([.. body, (byte)OpCode.GotoBack]);
    }

    private void GenerateForEachLoop(ForeachLoopStatementNode @foreach, ByteCodeOutput output, bool isInFunctionBody) {
        // loop:
        // next_or_end
        // assign
        // ...
        // goto loop
        ByteCodeOutput body = new(200);
        List<int> breakPositions = [];
        body.Add((byte)OpCode.Assign);
        body.AddRange(BitConverter.GetBytes(variableAddressLookup[@foreach.body.scope.GetVariableUnsafe(@foreach.value.name)]));
        GenerateBlock(@foreach.body, body, isInFunctionBody, true, CreateCurrentLoopSizeGetter(body), breakPositions);
        body.Add((byte)OpCode.GotoRelativeNoStackPush);
        body.AddRange(BitConverter.GetBytes(-(body.Count + 1)));
        output.Add((byte)OpCode.IterNextOrGotoRelative);
        output.AddRange(BitConverter.GetBytes(body.Count));
        output.AddRange(body);
    }

    private void GenerateForLoop(ForLoopStatementNode @for, ByteCodeOutput output, bool isInFunctionBody) {
        BlockStatementNode block = new([]);
        block.scope.parent = @for.body.scope.parent;
        @for.body.scope.parent = block.scope;

        if (@for.init is not null) {
            block.body.Add(@for.init);
        }
        BlockStatementNode body = @for.body ?? new([]);
        if (@for.update is not null)
            body.body.Add(new ExpressionStatementNode(@for.update));

        WhileLoopStatementNode @while = new(@for.condition ?? new IdentifierExpressionNode("true"), @for.body);
        block.body.Add(@while);
        GenerateBlock(block, output, isInFunctionBody, false, null, null);
        // ByteCodeOutput body = new(200);
        // List<int> breakPositions = [];
        // int conditionSize;
        // GenerateStatement(@for.body, body, isInFunctionBody, true, CreateCurrentLoopSizeGetter(body), breakPositions);

        // // init only allows a variable declaration
        // // the condtion can be null in which case, it's true
        // if (@for.condition is not null) {
        //     int currentSize = output.Count;
        //     GenerateExpression(@for.condition, output);
        //     conditionSize = output.Count - currentSize;
        // } else {
        //     // push true onto the stack
        //     // well, the address of true because that's what's needed for push
        //     output.Add((byte)OpCode.Push);
        //     output.AddRange(BitConverter.GetBytes(variableAddressLookup[@for.body.scope.GetVariableUnsafe("true")]));
        //     conditionSize = sizeof(Address) + sizeof(byte);
        // }

        // body.Add((byte)OpCode.GotoRelativeNoStackPush);
        // body.AddRange(BitConverter.GetBytes(-conditionSize)); // and that's why goto relative uses signed integers

        // // the body size + the next goto relative + relative addresses
        // int fullSize = body.Count + sizeof(byte) + sizeof(int) + breakPositions.Count * sizeof(int);
        // foreach (int index in breakPositions) {
        //     body.InsertRange(index, BitConverter.GetBytes(fullSize - index));
        // }

        // // skipp the body if the condition is false
        // output.Add((byte)OpCode.GotoRelativeIfNot);
        // output.AddRange(BitConverter.GetBytes(body.Count));
    }

    private void GenerateWhileLoop(WhileLoopStatementNode @while, ByteCodeOutput output, bool isInFunctionBody) {
        //loop:
        // check condition
        // push result
        // skipp if false
        //...
        //goto loop

        ByteCodeOutput body = new(200);
        ByteCodeOutput condition = new(20);
        List<int> breakPositions = [];

        // add the condtion
        GenerateExpression(@while.condition, condition);
        body.AddRange(condition);
        GenerateStatement(@while.body, body, isInFunctionBody, true, CreateCurrentLoopSizeGetter(body), breakPositions);

        // maybe this is correct
        int fullSize = body.Count - condition.Count + sizeof(int) * breakPositions.Count + 2 * (sizeof(byte) + sizeof(int));
        foreach (int index in breakPositions) {
            body.InsertRange(index, BitConverter.GetBytes(fullSize - index));
        }

        body.InsertRange(0, [(byte)OpCode.GotoRelativeIfNot, .. BitConverter.GetBytes(body.Count + 2 * (sizeof(byte) + sizeof(int)))]);
        body.Add((byte)OpCode.GotoRelativeNoStackPush);
        body.AddRange(BitConverter.GetBytes(-body.Count));

        output.AddRange(body);
    }

    private void GenerateIf(IfStatementNode @if, ByteCodeOutput output, bool isInFunctionBody, bool isInLoopBody, Func<int> getCurrentLoopBodySize, List<int> breakPositions) {

        // generate the byte code for the condition
        // in the vm, the resulting value will be pushed into the stack
        GenerateExpression(@if.condition, output);
        ByteCodeOutput @true = new(200);
        ByteCodeOutput @false = new(200);
        bool hasElse = @if.@false is not EmptyStatementNode or null;

        GenerateStatement(@if.@true, @true, isInFunctionBody, isInLoopBody, getCurrentLoopBodySize, breakPositions);
        if (hasElse)
            GenerateStatement(@if.@false, @false, isInFunctionBody, isInLoopBody, getCurrentLoopBodySize, breakPositions);

        // skipp the instructions from the false block
        @true.Add((byte)OpCode.GotoRelativeNoStackPush);
        @true.AddRange(BitConverter.GetBytes(@false.Count));

        // go to the next instruction if the condition is true
        // else skipp the instructions from the true block
        output.Add((byte)OpCode.GotoRelativeIfNot);
        output.AddRange(BitConverter.GetBytes(@true.Count));
        output.AddRange(@false);
        output.AddRange(@true);
    }
}