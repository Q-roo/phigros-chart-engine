using System;
using System.Diagnostics;
using System.Linq;
using DotNext.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

// TODO: dynamic is really hurting the preformace. Create a new wrapper object
using Value = CBObject;
using Result = Either<CBObject, ErrorType>;

public class ASTWalker {
    private readonly ASTRoot ast;
    private readonly Scope rootScope;
    private Scope currentScope;
    private bool isInFunction;

    public ASTWalker(ASTRoot ast) {
        this.ast = ast;
        rootScope = new(null);
        currentScope = rootScope;
        currentScope["dbg_print"] = new(new Func<Value[], Value>(args => {
            Godot.GD.Print(args);
            return new(ObjectValue.Unset);
        }));
    }

    public void Evaluate() {
        EvaluateRoot();
    }

    private void EvaluateRoot() {
        EvaluateBlock(ast);
    }

    private ObjectValueArray EvaluateArrayLiteral(ArrayLiteralExpressionNode arrayLiteral) => new(arrayLiteral.content.Map(it => EvaluateExpression(it).GetValue()).ToList());

    private Value EvaluateAssignment(AssignmentExpressionNode assignment) {
        Value asignee = EvaluateExpression(assignment.asignee);
        Value oldValue = new(asignee.GetValue());

        asignee.SetValue(
            assignment.@operator.Type switch {
                TokenType.DotAssign => EvaluateExpression(
                    new CallExpressionNode(
                        new ComputedMemberAccessExpressionNode(assignment.asignee, ((CallExpressionNode)assignment.value).method),
                        ((CallExpressionNode)assignment.value).arguments
                    )
                ).GetValue(),
                TokenType.Assign => EvaluateExpression(assignment.value).GetValue(),
                TokenType token => EvaluateBinary(new(new ValueExpressionNode<Value>(asignee), new Token(-1, -1, token switch {
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
                    _ => throw new NotSupportedException($"{token.ToSourceString()} cannot be turned into a binary operator"),
                }), assignment.value)).GetValue()
            }
        );

        return oldValue;
    }

    private Value EvaluateBinary(BinaryExpressionNode binary) => new(EvaluateExpression(binary.left).GetValue().ExecuteBinaryOperator(binary.@operator.Type, EvaluateExpression(binary.right).GetValue()));

    private Value EvaluateUnary(UnaryExpressionNode unary) => new(EvaluateExpression(unary.expression).GetValue().ExecuteUnaryOperator(unary.@operator.Type));

    private Value EvaluateExpression(ExpressionNode expression) {
        if (expression.IsNullOrEmpty())
            return null;

        return expression switch {
            ArrayLiteralExpressionNode arrayLiteral => new(EvaluateArrayLiteral(arrayLiteral)),
            AssignmentExpressionNode assignment => EvaluateAssignment(assignment),
            BinaryExpressionNode binary => EvaluateBinary(binary),
            CallExpressionNode call => EvaluateExpression(call.method).GetValue().AsCallable()(call.arguments.Map(it => EvaluateExpression(it).ShallowCopy()).ToArray()),
            ClosureExpressionNode closure => new(new Func<Value[], Value>(args => CallUserDefinedClosure(closure, args))),
            ComputedMemberAccessExpressionNode computedMemberAccess => EvaluateExpression(computedMemberAccess.member).GetValue().members[EvaluateExpression(computedMemberAccess.member).GetValue()],
            DoubleExpressionNode @double => new(@double.value),
            IdentifierExpressionNode identifier => currentScope[identifier.value],
            IntExpressionNode @int => new(@int.value),
            MemberAccessExpressionNode memberAccess => EvaluateExpression(new ComputedMemberAccessExpressionNode(memberAccess.member, new StringExpressionNode(memberAccess.property))),
            UnaryExpressionNode unary => EvaluateUnary(unary),
            RangeLiteralExpressionNode rangeLiteral => throw new NotImplementedException(),
            StringExpressionNode @string => new(@string.value),
            TernaryExpressionNode ternary => EvaluateExpression(ternary.condition).GetValue().AsBool() ? EvaluateExpression(ternary.@true) : EvaluateExpression(ternary.@false),
            ValueExpressionNode<Value> value => value.value,
            _ => throw new NotSupportedException($"evaluation of {expression.GetType()} is not supported")
        };
    }

    private Result EvaluateBlock(BlockStatementNode block) {
        currentScope = new(currentScope);

        foreach (StatementNode statement in block.body) {
            switch (EvaluateStatement(statement).Case) {
                case ErrorType.NoError:
                    break;
                case ErrorType error:
                    return error;
                case Value o:
                    if (isInFunction)
                        return o;

                    break;
                default:
                    throw new UnreachableException();
            }
        }

        currentScope = currentScope.parent;

        return ErrorType.NoError;
    }

    private ErrorType EvaluateCommand(CommandStatementNode command) {
        throw new NotImplementedException();
    }

    private Result EvaluateIf(IfStatementNode @if) {
        return EvaluateStatement(EvaluateExpression(@if.condition).GetValue().AsBool() ? @if.@true : @if.@false);
    }

    private Result EvaluateForEach(ForeachLoopStatementNode foreachLoop) {
        currentScope[foreachLoop.value.name] = null;
        foreach (ObjectValue it in (ObjectValueArray)EvaluateExpression(foreachLoop.iterable).GetValue()) {
            currentScope[foreachLoop.value.name] = new(it);

            switch (EvaluateStatement(foreachLoop.body).Case) {
                case ErrorType.NoError:
                    break;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType.ContinueLoop:
                    continue;
                case ErrorType error:
                    return error;
                case Value o:
                    if (isInFunction)
                        return o;

                    break;
                default:
                    throw new UnreachableException();
            }
        }

        return ErrorType.NoError;
    }

    private Result EvaluateFor(ForLoopStatementNode forLoop) {
        if (!forLoop.init.IsNullOrEmpty())
            DeclareVariable(forLoop.init);

        for (; forLoop.condition.IsNullOrEmpty() || EvaluateExpression(forLoop.condition).GetValue().AsBool(); EvaluateExpression(forLoop.update)) {
            switch (EvaluateStatement(forLoop.body).Case) {
                case ErrorType.NoError:
                    break;
                case ErrorType.ContinueLoop:
                    continue;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType error:
                    return error;
                case Value o:
                    if (isInFunction)
                        return o;

                    break;
                default:
                    throw new UnreachableException();
            }
        }

        return ErrorType.NoError;
    }

    private Result EvaluateWhile(WhileLoopStatementNode whileLoop) {
        while (true) {
            if (whileLoop.condition.IsNullOrEmpty() || EvaluateExpression(whileLoop.condition).GetValue().AsBool())
                return ErrorType.NoError;

            switch (EvaluateStatement(whileLoop.body).Case) {
                case ErrorType.NoError:
                    break;
                case ErrorType.ContinueLoop:
                    continue;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType error:
                    return error;
                case Value o:
                    if (isInFunction)
                        return o;

                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private ErrorType DeclareFunction(FunctionDeclarationStatementNode functionDeclaration) {
        return DeclareVariable(
            new(
                functionDeclaration.name,
                new ClosureExpressionNode(functionDeclaration.arguments, functionDeclaration.body, functionDeclaration.isLastParams)
            )
        );
    }

    private ErrorType DeclareVariable(VariableDeclarationStatementNode variableDeclaration) {
        currentScope.DeclareVariable(variableDeclaration.name, EvaluateExpression(variableDeclaration.valueExpression));
        return ErrorType.NoError;
    }

    private Result EvaluateReturn(ReturnStatementNode @return) {
        return EvaluateExpression(@return.value);
    }

    private Result EvaluateStatement(StatementNode statement) {
        if (statement.IsNullOrEmpty())
            return ErrorType.NoError;

        return statement switch {
            BlockStatementNode block => EvaluateBlock(block),
            BreakStatementNode => ErrorType.BreakLoop,
            CommandStatementNode command => EvaluateCommand(command),
            ContinueStatementNode => ErrorType.ContinueLoop,
            ExpressionStatementNode expression => EvaluateExpression(expression.expression),
            ForeachLoopStatementNode foreachLoop => EvaluateForEach(foreachLoop),
            ForLoopStatementNode forLoop => EvaluateFor(forLoop),
            FunctionDeclarationStatementNode functionDeclaration => DeclareFunction(functionDeclaration),
            IfStatementNode @if => EvaluateIf(@if),
            ReturnStatementNode @return => EvaluateReturn(@return),
            VariableDeclarationStatementNode variableDeclaration => DeclareVariable(variableDeclaration),
            WhileLoopStatementNode whileLoop => EvaluateWhile(whileLoop),
            _ => throw new NotSupportedException($"evaluation of {statement.GetType()} is not supported"),
        };
    }

    private Value CallUserDefinedClosure(ClosureExpressionNode closure, params Value[] args) {
        // declarations
        Scope scope = new(currentScope);
        Value result = new(ObjectValue.Unset);

        // temporary function scope
        currentScope = scope;
        isInFunction = true;

        if (!closure.isLastParams)
            for (int i = 0; i < closure.arguments.Length; i++)
                scope.DeclareVariable(closure.arguments[i].name, args[i]);
        else {
            for (int i = 0; i < closure.arguments.Length - 1; i++)
                scope.DeclareVariable(closure.arguments[i].name, args[i]);

            scope.DeclareVariable(closure.arguments[^1].name, new(new ObjectValueArray(args.Slice(new(closure.arguments.Length - 1, args.Length - 1)).Map(it => it.GetValue()).ToList())));
        }


        // cannot use evaluate block because a new scope is already declared
        if (closure.body is BlockStatementNode block) {
            foreach (StatementNode statement in block.body) {
                switch (EvaluateStatement(statement).Case) {
                    case ErrorType.NoError:
                        break;
                    // case ErrorType error:
                    //     return error;
                    case Value o:
                        currentScope = scope.parent;
                        isInFunction = false;
                        return o;
                    default:
                        throw new UnreachableException();
                }
            }
        }

        // switch back to the previous scope
        currentScope = scope.parent;
        isInFunction = false;

        return result;
    }
}