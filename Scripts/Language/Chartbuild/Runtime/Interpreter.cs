using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;


// as it turns out, scopes have to be reconstructed for nested functions
// and keeping track of them would be a pain
// this will walk down the ast and won't remove variable and function declarations
// but it will do the other optimizations thought
// it will also reconstruct scopes regularly
// use this to alalyze and optimize the ast
// after that, the byte code generator can remove the variable and function declarations
public class Interpreter(ASTRoot ast) {
    readonly ASTRoot ast = ast;
    private Scope RootScope => ast.scope;

    public static Either<ICBValue, ErrorType> Evaluate(StatementNode statement, Scope scope) => EvaluateStatement(statement, scope);

    public ASTRoot Analyze() {
        InitalizeRootScope();

        if (ast.body.Count > 0)
            if (ast.body[0] is CommandStatementNode command)
                if (command.name == "version") {
                    AnalyzeBlockLike(ast, false, false);
                    return ast;
                }

        PushError(ErrorType.DoesNotStartWithVersion, "chart build scripts must start with defining a version");
        return ast;
    }

    private void InitalizeRootScope() {
        ast.scope = CreateScope(null);
        SetupRootScope();
    }

    private void SetupRootScope() {
        RootScope.DeclareVariable("unset", new NullValue(), true);
        RootScope.DeclareVariable("true", new BoolValue(true), true);
        RootScope.DeclareVariable("false", new BoolValue(false), true);

        RootScope.DeclareNativeMethod(
            "dbg_print",
            args => GD.Print(string.Join<ICBValue>(", ", args)),
            false,
            new()
            {
                {"input", new AnyType()}
            },
            true
        );
    }

    private ErrorType PushError(ErrorType error) => PushError(error, error.ToString());

    private ErrorType PushError(ErrorType error, string message) {
        ast.errors.Add(new(error, message, -1, -1));
        return error;
    }

    private static Scope CreateScope(Scope parent) {
        return new(parent);
    }

    private static Either<ICBValue, ErrorType> EvaluateExpression(ExpressionNode expression, Scope scope) => expression switch {
        StringExpressionNode @string => new StringValue(@string.value),
        IntExpressionNode @int => new I32Value(@int.value),
        DoubleExpressionNode @float => new F32Value(@float.value),
        ValueExpressionNode<ICBValue> value => Either<ICBValue, ErrorType>.Left(value.value), // analyzer can produce these during constant folding
        BinaryExpressionNode binary => EvaluateExpression(binary.left, scope)
                                        .Match(err => err,
                                        new Func<ICBValue, Either<ICBValue, ErrorType>>(
                                            lhs => EvaluateExpression(binary.right, scope)
                                            .Match(
                                                err => err,
                                                rhs => lhs.ExecuteBinaryOperator(binary.@operator.Type, lhs)
                                            )
                                        )),
        AssignmentExpressionNode assignment => ErrorType.NotCompileTimeConstant, //EvaluateAssignmentOperation(assignment, scope),
        IdentifierExpressionNode identifier => scope.GetVariable(identifier.value).Case switch {
            CBVariable value when !value.constant => ErrorType.NotCompileTimeConstant,
            CBVariable value => value.GetValue(),
            ErrorType err => err,
            _ => throw new UnreachableException()
        },
        ArrayLiteralExpressionNode array => EvaluateArray(array, scope),
        CallExpressionNode call => EvaluateCall(call, scope),
        ClosureExpressionNode => ErrorType.NotSupported, // TODO
        MemberAccessExpressionNode access => EvaluateMemberAccess(access, scope),
        ComputedMemberAccessExpressionNode access => EvaluateComputedMemberAccess(access, scope),
        RangeLiteralExpressionNode => ErrorType.NotSupported, // TODO: step
        TernaryExpressionNode ternary => EvaluateTernary(ternary, scope),
        _ => throw new NotImplementedException($"TODO: evaluate {expression.GetType()} if constant")
    };

    private static Either<ICBValue, ErrorType> EvaluateArray(ArrayLiteralExpressionNode arrayLiteral, Scope scope) {
        ArrayValue array = new();
        foreach (ExpressionNode expression in arrayLiteral.content)
            switch (EvaluateExpression(expression, scope).Case) {
                case ICBValue value:
                    ErrorType error = array.AddMember(value);
                    if (error != ErrorType.NoError)
                        return error;

                    break;
                case ErrorType err:
                    return err;
            }

        return array;
    }

    private static Either<ICBValue, ErrorType> EvaluateCall(CallExpressionNode call, Scope scope) {
        switch (EvaluateExpression(call.method, scope).Case) {
            case ICallableICBValue callable:
                if (!callable.IsPureCallable)
                    return ErrorType.NotCompileTimeConstant;

                List<ICBValue> args = new(call.arguments.Length);
                foreach (ExpressionNode expression in call.arguments) {
                    switch (EvaluateExpression(expression, scope).Case) {
                        case ICBValue value:
                            // NOTE: while type checking could be done here for the arguments, it will be done during the call
                            // because that's when native functions check the argument types as well
                            args.Add(value);
                            break;
                        case ErrorType err:
                            return err;
                        default:
                            throw new UnreachableException();
                    }
                }
                // type checking be handled by the function
                return callable.Call([.. args]);/* .Case switch {
                    ICBValue result => result.Type.CanBeAssignedTo(callable.ReturnType) ? callable.ReturnType.Constructor(result) : ErrorType.InvalidType,
                    ErrorType err => err,
                    _ => throw new UnreachableException()
                }; */
            case ICBValue:
                return ErrorType.NotCallable;
            case ErrorType err:
                return err;
            default:
                throw new UnreachableException();
        }
    }

    private static Either<ICBValue, ErrorType> EvaluateTernary(TernaryExpressionNode ternary, Scope scope) {
        switch (EvaluateExpression(ternary.condition, scope).Case) {
            case ICBValue value:
                if (!value.Type.CanBeAssignedTo(new BoolType()))
                    return ErrorType.InvalidType;

                return value.TryCast<BoolValue>().Case switch {
                    BoolValue @bool => EvaluateExpression(@bool ? ternary.@true : ternary.@false, scope),
                    ErrorType err => (Either<ICBValue, ErrorType>)err,
                    _ => throw new UnreachableException(),
                };
            case ErrorType err:
                return err;
            default:
                throw new UnreachableException();
        }
    }

    private static Either<ICBValue, ErrorType> EvaluateMemberAccess(MemberAccessExpressionNode memberAccess, Scope scope) {
        return EvaluateExpression(memberAccess.member, scope).Case switch {
            ICBValue member => member.GetMember(new StringValue(memberAccess.property)),
            ErrorType err => err,
            _ => throw new UnreachableException(),
        };
    }

    private static Either<ICBValue, ErrorType> EvaluateComputedMemberAccess(ComputedMemberAccessExpressionNode computedMemberAccess, Scope scope) {
        return EvaluateExpression(computedMemberAccess.member, scope).Case switch {
            ICBValue member => EvaluateExpression(computedMemberAccess.property, scope).Case switch {
                ICBValue property => member.GetMember(property),
                ErrorType err => err,
                _ => throw new UnreachableException()
            },
            ErrorType err => err,
            _ => throw new UnreachableException(),
        };
    }

    private static Either<ICBValue, ErrorType> EvaluateStatement(BlockStatementNode block) => EvaluateStatement(block, block.scope);

    private static Either<ICBValue, ErrorType> EvaluateStatement(StatementNode statement, Scope scope) => statement switch {
        CommandStatementNode => ErrorType.UnexpectedToken, // TODO, also, should it really be possible for commands to be anywhere else other than the top level
        BlockStatementNode block => EvaluateBlockStatement(block, scope),
        FunctionDeclarationStatementNode functionDeclaration => EvaluateFunctionDeclaration(functionDeclaration, scope),
        VariableDeclarationStatementNode variableDeclaration => EvaluateVariableDeclaration(variableDeclaration, scope),
        BreakStatementNode => ErrorType.BreakLoop,
        ReturnStatementNode @return => EvaluateReturnStatement(@return, scope),
        ContinueStatementNode => ErrorType.ContinueLoop,
        ExpressionStatementNode expression => EvaluateExpression(expression.expression, scope).Case switch { // only a return statement can return a value
            ICBValue => ErrorType.NoError,
            ErrorType err => err,
            _ => throw new UnreachableException()
        },
        // ForeachLoopStatementNode @foreach => EvaluateForeachLoopStatement(@foreach, scope),
        // ForLoopStatementNode @for => EvaluateForLoopStatement(@for, scope),
        // WhileLoopStatementNode @while => EvaluateWhileLoopStatement(@while, scope),
        // IfStatementNode @if => EvaluateIfStatement(@if, scope),
        // EmptyStatementNode
        _ => throw new NotImplementedException($"TODO: evaluate {statement.GetType()}")
    };

    private static Either<ICBValue, ErrorType> EvaluateBlockStatement(BlockStatementNode block, Scope scope) {
        // reset the scope
        block.scope = CreateScope(scope);
        foreach (StatementNode statement in block.body)
            switch (EvaluateStatement(statement, scope).Case) {
                case ICBValue v:
                    return Either<ICBValue, ErrorType>.Left(v);
                case ErrorType err when err is not ErrorType.NoError:
                    return err;
            }

        return ErrorType.NoError;
    }

    private static Either<ICBValue, ErrorType> EvaluateFunctionDeclaration(FunctionDeclarationStatementNode functionDeclaration, Scope scope) {
        functionDeclaration.body.scope = CreateScope(scope);

        foreach (FunctionParameter parameter in functionDeclaration.arguments)
            if (functionDeclaration.body.scope.DeclareVariable(parameter.name, new NullValue(), false) != ErrorType.NoError)
                throw new Exception("couldn't declare function parameters");

        DeclaredFunction function = new() {
            returnType = functionDeclaration.returnType,
            isLastParams = functionDeclaration.isLastParams,
            parameterNames = functionDeclaration.arguments.Select(it => it.name).ToArray(),
            paremeterTypes = functionDeclaration.arguments.Select(it => it.type).ToArray(),
            pure = functionDeclaration.pure,
            body = functionDeclaration.body,
        };

        if (scope.HasVariable(functionDeclaration.name))
            scope.GetVariableUnsafe(functionDeclaration.name).GetValueUnsafe().SetValue(function);

        else {
            ErrorType error = scope.DeclareVariable(
                functionDeclaration.name,
                new CBFunctionValue(function),
                true
            );

            if (error != ErrorType.NoError)
                throw new Exception("couldn't declare function");
        }

        return ErrorType.NoError;
    }

    private static Either<ICBValue, ErrorType> EvaluateVariableDeclaration(VariableDeclarationStatementNode variableDeclaration, Scope scope) {
        ICBValue value = null;

        if (!variableDeclaration.valueExpression.IsNullOrEmpty())
            switch (EvaluateExpression(variableDeclaration.valueExpression, scope).Case) {
                case ICBValue result:
                    variableDeclaration.type ??= result.Type;
                    if (!result.Type.CanBeAssignedTo(variableDeclaration.type))
                        return ErrorType.InvalidType;

                    switch (variableDeclaration.type.Constructor(result).Case) {
                        case ICBValue casted:
                            value = casted;
                            break;
                        case ErrorType err:
                            return err;
                    }
                    break;
                case ErrorType err:
                    return err;
            }

        ErrorType error = scope.DeclareVariable(variableDeclaration.name, value, variableDeclaration.@readonly);

        if (error != ErrorType.NoError)
            return error;

        return ErrorType.NoError;
    }

    private static Either<ICBValue, ErrorType> EvaluateReturnStatement(ReturnStatementNode @return, Scope scope) {
        if (@return.value is not null)
            return EvaluateExpression(@return.value, scope);

        return scope.GetVariable("unset").Case switch {
            ICBValue v => Either<ICBValue, ErrorType>.Left(v),
            _ => throw new UnreachableException()
        };
    }

    // assumes that the scope is already set up
    private T AnalyzeBlockLike<T>(T block, bool isInFunctionBody, bool isInLoopBody) where T : BlockStatementNode {
        for (int i = 0; i < block.body.Count; i++) {
            block.body[i] = AnalyzeStatement(block.body[i], block.scope, isInFunctionBody, isInLoopBody);
            if (block.body[i].IsNullOrEmpty()) {
                block.body.RemoveAt(i);
                i--;
            }
        }

        return block;
    }

    private StatementNode AnalyzeStatement(StatementNode statement, Scope scope, bool isInFunctionBody, bool isInLoopBody) {
        switch (statement) {
            case CommandStatementNode:
                PushError(ErrorType.NotSupported, "TODO");
                break;
            case ExpressionStatementNode expression:
                expression.expression = AnalyzeExpression(expression.expression, scope);
                // the expression's abscence won't change how the program works
                if (expression.expression.IsNullOrEmpty())
                    return new EmptyStatementNode();

                if (EvaluateExpression(expression.expression, scope) != ErrorType.NotCompileTimeConstant)
                    return new EmptyStatementNode();
                break;
            case BlockStatementNode block: {
                block.scope = CreateScope(scope);
                block = AnalyzeBlockLike(block, isInFunctionBody, isInLoopBody);
                if (block.body.Count == 0)
                    return new EmptyStatementNode();

                break;
            }
            case VariableDeclarationStatementNode variableDeclaration:
                // can't do much more because the scopes are going to need it during declaration
                if (!variableDeclaration.valueExpression.IsNullOrEmpty())
                    variableDeclaration.valueExpression = AnalyzeExpression(variableDeclaration.valueExpression, scope);

                switch (EvaluateExpression(variableDeclaration.valueExpression, scope).Case) {
                    case ICBValue v:
                        scope.DeclareVariable(variableDeclaration.name, v, variableDeclaration.@readonly);
                        break;
                }

                break;
            case FunctionDeclarationStatementNode functionDeclaration:
                functionDeclaration.body.scope = CreateScope(scope);

                foreach (FunctionParameter parameter in functionDeclaration.arguments)
                    if (functionDeclaration.body.scope.DeclareVariable(parameter.name, new NullValue(), false) != ErrorType.NoError)
                        throw new Exception("couldn't declare function parameters");

                // declare the function first
                // this is needed for e.g. recursion
                scope.DeclareVariable(functionDeclaration.name, new CBFunctionValue(new DeclaredFunction() {
                    returnType = functionDeclaration.returnType,
                    isLastParams = functionDeclaration.isLastParams,
                    parameterNames = functionDeclaration.arguments.Select(it => it.name).ToArray(),
                    paremeterTypes = functionDeclaration.arguments.Select(it => it.type).ToArray(),
                    pure = functionDeclaration.pure,
                    body = functionDeclaration.body,
                }
                ), true);

                // don't allow the following
                // while (true) {
                // ||{break;}
                // }
                AnalyzeBlockLike(functionDeclaration.body, true, false);

                if (functionDeclaration.isLastParams)
                    functionDeclaration.arguments[^1].type = new ArrayType(functionDeclaration.arguments[^1].type);

                // attempt to evaluate the function
                // the arguments are all uninitalized so the null value error might not be an error (when the arguments are initalized)
                bool pure = EvaluateStatement(functionDeclaration.body).Case switch {
                    ErrorType.MissingMember or
                    ErrorType.NoError or ErrorType.NullValue => true, // there should be only 4+1 cases
                    ErrorType => false,                               // 1: null value error (the function parameter values are set to null at this point)
                                                                      // 2: no error but no value to return
                                                                      // 3: no error and there is a value to return
                                                                      // 4: not compile time constant AKA, not pure
                                                                      // 5: invalid syntax, types and such in which case, the code won't even run
                    _ => true
                };
                functionDeclaration.pure = pure;
                functionDeclaration.returnType ??= new NullType();

                break;
            case ReturnStatementNode @return:
                if (!isInFunctionBody)
                    PushError(ErrorType.UnexpectedToken, "return statements can only be used inside functions");

                if (!@return.value.IsNullOrEmpty())
                    @return.value = AnalyzeExpression(@return.value, scope);

                break;
            case BreakStatementNode:
                if (!isInLoopBody)
                    PushError(ErrorType.UnexpectedToken, "break statements can only be used inside loops");

                break;
            case ContinueStatementNode:
                if (!isInLoopBody)
                    PushError(ErrorType.UnexpectedToken, "continue statements can only be used inside loops");

                break;
            case IfStatementNode @if:
                // if(cond);
                // else {...}
                if (@if.@true.IsNullOrEmpty()) {
                    @if.condition = new PrefixExpressionNode(new Token(-1, -1, TokenType.Not), @if.condition);
                    @if.@true = @if.@false;
                    @if.@false = @if.@true;
                }

                @if.condition = AnalyzeExpression(@if.condition, scope);

                // case
                // if(NonPure() or pure);
                // else;
                if (@if.@true.IsNullOrEmpty() && @if.@false.IsNullOrEmpty())
                    switch (EvaluateExpression(@if.condition, scope).Case) {
                        case ICBValue:
                            return new EmptyStatementNode();
                        case ErrorType err when err is not ErrorType.NotCompileTimeConstant:
                            PushError(err);
                            break;
                        default:
                            return new ExpressionStatementNode(@if.condition);
                    }

                switch (EvaluateExpression(@if.condition, scope).Case) {
                    case ICBValue value:
                        if (!value.Type.CanBeAssignedTo(new BoolType()))
                            PushError(ErrorType.InvalidType, "if statements need a boolean for the condition");

                        switch (value.TryCast<BoolValue>().Case) {
                            // one of the either
                            // if(true)
                            // if (false)
                            case BoolValue @bool:
                                return AnalyzeStatement(@bool ? @if.@true : @if.@false, scope, isInFunctionBody, isInLoopBody);
                            case ErrorType err:
                                PushError(err);
                                break;
                        }

                        break;
                    case ErrorType.NotCompileTimeConstant:
                        break;
                    case ErrorType err:
                        PushError(err);
                        break;
                    default:
                        throw new UnreachableException();
                }
                break;
            case ForLoopStatementNode forLoop: {
                // turn the for loop into a while loop
                // for (init;condition;update)body
                // {
                //   init;
                //   while(condition)
                //   body
                //   update;
                // }
                BlockStatementNode block = new([]) {
                    scope = CreateScope(scope)
                };

                if (!forLoop.init.IsNullOrEmpty())
                    block.body.Add(AnalyzeStatement(forLoop.init, block.scope, isInFunctionBody, isInLoopBody));

                WhileLoopStatementNode whileLoop = new(
                    AnalyzeExpression(forLoop.condition ?? new IdentifierExpressionNode("true"), scope),
                                                                     // the analyzer should be able to remove this
                                                                     // if it's null or empty
                    new BlockStatementNode([..forLoop.body.body, new ExpressionStatementNode(forLoop.update)])
                );
                return AnalyzeStatement(whileLoop, scope, isInFunctionBody, isInLoopBody);
            }
            case ForeachLoopStatementNode:
                // TODO: ICBValueIEnumeratorValue
                throw new NotImplementedException("TODO");
            case WhileLoopStatementNode whileLoop:
                whileLoop.condition = AnalyzeExpression(whileLoop.condition, scope);
                whileLoop.body = AnalyzeBlockLike(whileLoop.body, isInFunctionBody, true);
                break;
            case EmptyStatementNode:
                return statement;
            default:
                throw new Exception($"unknown statement: {statement}");
        }
        return statement;
    }

    private ExpressionNode AnalyzeExpression(ExpressionNode expression, Scope scope) {
        switch (expression) {
            case ArrayLiteralExpressionNode array:
                for (int i = 0; i < array.content.Length; i++)
                    array.content[i] = AnalyzeExpression(array.content[i], scope);

                break;
            case AssignmentExpressionNode assignment:
                assignment.asignee = AnalyzeExpression(assignment.asignee, scope);
                assignment.value = AnalyzeExpression(assignment.value, scope);
                // replace assignment with empty
                // TODO: assignment doesn't change the value and doesn't cause side effects
                break;
            case BinaryExpressionNode binary:
                binary.left = AnalyzeExpression(binary.left, scope);
                binary.right = AnalyzeExpression(binary.right, scope);
                // try evaluating the binary expression
                // if it can evaluate to a value, the expression can be folded
                switch (EvaluateExpression(binary, scope).Case) {
                    case ICBValue value:
                        return new ValueExpressionNode<ICBValue>(value);
                }
                break;
            case CallExpressionNode call:
                for (int i = 0; i < call.arguments.Length; i++)
                    call.arguments[i] = AnalyzeExpression(call.arguments[i], scope);

                call.isNative = EvaluateExpression(call.method, scope).Case switch {
                    CBFunctionValue functionValue => functionValue.value is NativeFunctionBinding,
                    _ => false
                };

                switch (EvaluateCall(call, scope).Case) {
                    case ICBValue value: // the function is a pure function and can be safely evaluated during compile time
                        return new ValueExpressionNode<ICBValue>(value);
                }
                break;
            case ClosureExpressionNode closure:
                // TODO
                break;
            case ComputedMemberAccessExpressionNode computedMemberAccess:
                computedMemberAccess.member = AnalyzeExpression(computedMemberAccess.member, scope); // TODO: see if this could cause any bugs
                computedMemberAccess.property = AnalyzeExpression(computedMemberAccess.property, scope);
                break;
            case MemberAccessExpressionNode memberAccess:
                memberAccess.member = AnalyzeExpression(memberAccess.member, scope);
                break;
            case UnaryExpressionNode unary: // handles both posfix and prefix
                unary.expression = AnalyzeExpression(unary.expression, scope);
                break;
            case RangeLiteralExpressionNode rangeLiteral:
                rangeLiteral.start = AnalyzeExpression(rangeLiteral.start, scope);
                rangeLiteral.end = AnalyzeExpression(rangeLiteral.end, scope);
                break;
            case TernaryExpressionNode ternary:
                ternary.@true = AnalyzeExpression(ternary.@true, scope);
                ternary.@false = AnalyzeExpression(ternary.@false, scope);
                ternary.condition = AnalyzeExpression(ternary.condition, scope);
                switch (EvaluateExpression(ternary.condition, scope).Case) {
                    case BoolValue @bool:
                        return @bool ? ternary.@true : ternary.@false;
                }
                break;
            // the rest are literals
            // or identifiers
            default:
                break;
        }
        // throw new NotImplementedException("TODO");
        return expression;
    }
}