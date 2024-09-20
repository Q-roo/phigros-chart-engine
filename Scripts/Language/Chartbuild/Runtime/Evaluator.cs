using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

public static class Evaluator {
    public static Either<ICBValue, ErrorType> Evaluate(this ExpressionNode expression, Scope scope) => EvaluateExpression(expression, scope);
    public static Either<ICBValue, ErrorType> Evaluate(this StatementNode statement, Scope scope) => EvaluateStatement(statement, scope);


    public static Either<ICBValue, ErrorType> EvaluateExpression(ExpressionNode expression, Scope scope) => expression switch {
        StringExpressionNode @string => new StringValue(@string.value),
        IntExpressionNode @int => new I32Value(@int.value),
        DoubleExpressionNode @float => new F32Value(@float.value),
        ValueExpressionNode<ICBValue> value => Either<ICBValue, ErrorType>.Left(value.value), // analyzer can produce these during constant folding
        BinaryExpressionNode binary => EvaluateBinaryOperation(binary, scope),
        AssignmentExpressionNode assignment => ErrorType.NotCompileTimeConstant, //EvaluateAssignmentOperation(assignment, scope),
        IdentifierExpressionNode identifier => scope.GetVariable(identifier.value).Case switch {
            CBVariable value when !value.constant => ErrorType.NotCompileTimeConstant,
            CBVariable value => value.GetValue(),
            ErrorType err => err,
            _ => throw new UnreachableException()
        },
        ArrayLiteralExpressionNode array => EvaluateArray(array, scope).MapLeft<ICBValue>(v => v),
        CallExpressionNode call => EvaluateCall(call, scope),
        ClosureExpressionNode => ErrorType.NotSupported, // TODO
        MemberAccessExpressionNode access => EvaluateMemberAccess(access, scope),
        ComputedMemberAccessExpressionNode access => EvaluateComputedMemberAccess(access, scope),
        RangeLiteralExpressionNode => ErrorType.NotSupported, // TODO: step
        TernaryExpressionNode ternary => EvaluateTernary(ternary, scope),
        _ => throw new NotImplementedException($"TODO: evaluate {expression.GetType()} if constant")
    };

    public static Either<ICBValue, ErrorType> EvaluateBinaryOperation(BinaryExpressionNode binaryExpression, Scope scope) {
        return EvaluateExpression(binaryExpression.left, scope).Case switch {
            ICBValue left => EvaluateExpression(binaryExpression.right, scope).Case switch {
                ICBValue right => left.ExecuteBinaryOperator(binaryExpression.@operator.Type, right).Case switch {
                    ICBValue v => Either<ICBValue, ErrorType>.Left(v),
                    // this should work: 0 == 0.0
                    // because i32 can be assigned to f32
                    // so cast the left side
                    ErrorType.InvalidType when left.Type.CanBeAssignedTo(right.Type) => right.Type.Constructor(left).Case switch {
                        ICBValue casted => casted.ExecuteBinaryOperator(binaryExpression.@operator.Type, right),
                        ErrorType err => err,
                        _ => throw new UnreachableException()
                    },
                    ErrorType err => err,
                    _ => throw new UnreachableException()
                },
                ErrorType err => err,
                _ => throw new UnreachableException()
            },
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }

    public static Either<ICBValue, ErrorType> EvaluateGetVariable(IdentifierExpressionNode identifier, Scope scope) {
        return scope.GetVariable(identifier.value).Case switch {
            CBVariable value when !value.constant => ErrorType.NotCompileTimeConstant,
            CBVariable value => value.GetValue(),
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }

    public static Either<ArrayValue, ErrorType> EvaluateArray(ArrayLiteralExpressionNode arrayExpression, Scope scope) {
        ArrayValue array = new();
        foreach (ExpressionNode expression in arrayExpression.content) {
            switch (EvaluateExpression(expression, scope).Case) {
                case ICBValue value: {
                    ErrorType error = array.AddMember(value);
                    if (error != ErrorType.NoError)
                        return error;
                }
                break;
                case ErrorType error: {
                    return error;
                }
            }
        }

        return array;
    }

    public static Either<ICBValue, ErrorType> EvaluateCall(CallExpressionNode call, Scope scope) {
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
            case ICBValue value:
                return ErrorType.NotCallable;
            case ErrorType err:
                return err;
            default:
                throw new UnreachableException();
        }
    }

    public static Either<ICBValue, ErrorType> EvaluateTernary(TernaryExpressionNode ternary, Scope scope) {
        return EvaluateExpression(ternary.condition, scope).Case switch {
            BoolValue @bool => @bool ? EvaluateExpression(ternary.@true, scope) : EvaluateExpression(ternary.@false, scope),
            ICBValue => ErrorType.InvalidType,
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }

    public static Either<ICBValue, ErrorType> EvaluateMemberAccess(MemberAccessExpressionNode memberAccess, Scope scope) {
        return EvaluateExpression(memberAccess.member, scope).Case switch {
            ICBValue member => member.GetMember(new StringValue(memberAccess.property)),
            ErrorType err => err,
            _ => throw new UnreachableException(),
        };
    }

    public static Either<ICBValue, ErrorType> EvaluateComputedMemberAccess(ComputedMemberAccessExpressionNode computedMemberAccess, Scope scope) {
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

    // return statements can return values which will buble up
    public static Either<ICBValue, ErrorType> EvaluateStatement(StatementNode statement, Scope scope) => statement switch {
        BlockStatementNode block => EvaluateBlockStatement(block, scope),
        BreakStatementNode => ErrorType.BreakLoop,
        CommandStatementNode => ErrorType.UnexpectedToken, // TODO, also, should it really be possible for commands to be anywhere else other than the top level
        ContinueStatementNode => ErrorType.ContinueLoop,
        ExpressionStatementNode expression => EvaluateExpression(expression.expression, scope).Case switch { // only a return statement can return a value
            ICBValue => ErrorType.NoError,
            ErrorType err => err,
            _ => throw new UnreachableException()
        },
        ForeachLoopStatementNode @foreach => EvaluateForeachLoopStatement(@foreach, scope),
        ForLoopStatementNode @for => EvaluateForLoopStatement(@for, scope),
        WhileLoopStatementNode @while => EvaluateWhileLoopStatement(@while, scope),
        IfStatementNode @if => EvaluateIfStatement(@if, scope),
        ReturnStatementNode @return => EvaluateReturnStatement(@return, scope),
        // these should have been removed
        // FunctionDeclarationStatementNode
        // VariableDeclarationStatementNode
        // EmptyStatementNode
        _ => throw new NotImplementedException($"TODO: evaluate {statement.GetType()}")
    };

    public static Either<ICBValue, ErrorType> EvaluateBlockStatement(BlockStatementNode block, Scope scope) {
        foreach (StatementNode statement in block.body)
            switch (EvaluateStatement(statement, scope).Case) {
                case ICBValue v:
                    return Either<ICBValue, ErrorType>.Left(v);
                case ErrorType err when err is not ErrorType.NoError:
                    return err;
            }

        return ErrorType.NoError;
    }

    public static Either<ICBValue, ErrorType> EvaluateForeachLoopStatement(ForeachLoopStatementNode @foreach, Scope scope) {
        switch (EvaluateExpression(@foreach.iterable, scope).Case) {
            case ArrayValue array: // arrays are the only iterable collections for now
                switch (@foreach.body.scope.GetVariable(@foreach.value.name).Case) {
                    case CBVariable variable:
                        foreach (ICBValue value in array.values) {
                            ErrorType error = variable.SetValue(value);
                            if (error != ErrorType.NoError)
                                return error;

                            switch (EvaluateBlockStatement(@foreach.body, @foreach.body.scope).Case) {
                                case ICBValue v:
                                    return Either<ICBValue, ErrorType>.Left(v);
                                case ErrorType.ContinueLoop or ErrorType.NoError:
                                    break;
                                case ErrorType.BreakLoop:
                                    return ErrorType.NoError;
                                case ErrorType err:
                                    return err;
                            }
                        }
                        break;
                    case ErrorType err:
                        return err;
                    default:
                        throw new UnreachableException();
                }
                break;
            case ICBValue:
                return ErrorType.InvalidType;
            case ErrorType err:
                return err;
        }

        return ErrorType.NoError;
    }

    public static Either<ICBValue, ErrorType> EvaluateForLoopStatement(ForLoopStatementNode @for, Scope scope) {
        while (true) {
            if (@for.condition is not null) {
                switch (EvaluateExpression(@for.condition, @for.body.scope).Case) {
                    case BoolValue @bool:
                        if (!@bool)
                            return ErrorType.NoError;
                        break;
                    case ICBValue:
                        return ErrorType.InvalidType;
                    case ErrorType err:
                        return err;
                }
            }

            switch (EvaluateBlockStatement(@for.body, @for.body.scope).Case) {
                case ICBValue v:
                    return Either<ICBValue, ErrorType>.Left(v);
                case ErrorType.ContinueLoop or ErrorType.NoError:
                    break;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType err:
                    return err;
                default:
                    if (@for.update is not null)
                        switch (EvaluateExpression(@for.update, @for.body.scope).Case) {
                            case ErrorType err when err is not ErrorType.NoError:
                                return err;
                        }
                    break;
            }
        }
    }

    public static Either<ICBValue, ErrorType> EvaluateWhileLoopStatement(WhileLoopStatementNode @while, Scope scope) {
        while (true) {
            if (@while.condition is not null)
                switch (EvaluateExpression(@while.condition, scope).Case) {
                    case BoolValue @bool:
                        if (!@bool)
                            return ErrorType.NoError;
                        break;
                    case ICBValue:
                        return ErrorType.InvalidType;
                    case ErrorType err:
                        return err;
                }

            switch (EvaluateBlockStatement(@while.body, @while.body.scope).Case) {
                case ICBValue v:
                    return Either<ICBValue, ErrorType>.Left(v);
                case ErrorType.ContinueLoop or ErrorType.NoError:
                    break;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType err:
                    return err;
            }
        }
    }

    public static Either<ICBValue, ErrorType> EvaluateIfStatement(IfStatementNode @if, Scope scope) {
        return EvaluateExpression(@if.condition, scope).Case switch {
            BoolValue @bool => EvaluateStatement(@bool ? @if.@true : @if.@false, scope),
            ICBValue => ErrorType.InvalidType,
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }

    public static Either<ICBValue, ErrorType> EvaluateReturnStatement(ReturnStatementNode @return, Scope scope) {
        if (@return.value is not null)
            return EvaluateExpression(@return.value, scope);

        ICBValue value = scope.GetVariable("unset").Case switch {
            ICBValue v => v,
            _ => throw new UnreachableException()
        };

        return Either<ICBValue, ErrorType>.Left(value);
    }
}