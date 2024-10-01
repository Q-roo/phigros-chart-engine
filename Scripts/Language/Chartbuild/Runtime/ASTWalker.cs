using System;
using System.Collections.Generic;
using System.Linq;
using DotNext.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

// TODO: dynamic is really hurting the preformace. Create a new wrapper object
using Value = dynamic;
using Result = Either<dynamic, ErrorType>;

public class ASTWalker {
    private readonly ASTRoot ast;
    private readonly Scope rootScope;
    private Scope currentScope;

    public ASTWalker(ASTRoot ast) {
        this.ast = ast;
        rootScope = new(null);
        currentScope = rootScope;
        currentScope["dbg_print"] = new Func<Value[], Value>(args => {
            Godot.GD.Print(args);
            return null;
        });
    }

    public void Evaluate() {
        EvaluateRoot();
    }

    private void EvaluateRoot() {
        EvaluateBlock(ast);
    }

    private List<Value> EvaluateArrayLiteral(ArrayLiteralExpressionNode arrayLiteral) {
        return arrayLiteral.content.Map(EvaluateExpression).ToList();
    }

    private Value EvaluateAssignment(AssignmentExpressionNode assignment) {
        Value key = EvaluateExpression(assignment.asignee);
        Value value = currentScope[key];

        currentScope[key] = assignment.@operator.Type switch {
            TokenType.DotAssign => EvaluateExpression(new CallExpressionNode(
                new ComputedMemberAccessExpressionNode(
                    assignment.asignee,
                    ((CallExpressionNode)assignment.value).method
                ),
                ((CallExpressionNode)assignment.value).arguments
            )),
            TokenType.Assign => EvaluateExpression(assignment.value),
            TokenType token => EvaluateBinary(new(value, new Token(-1, -1, token switch {
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
            }), assignment.value))
        };

        return value;
    }

    private Value EvaluateBinary(BinaryExpressionNode binaryExpression) {
        return binaryExpression.@operator.Type switch {
            TokenType.LessThan => EvaluateExpression(binaryExpression.left) < EvaluateExpression(binaryExpression.right),
            TokenType.LessThanOrEqual => EvaluateExpression(binaryExpression.left) <= EvaluateExpression(binaryExpression.right),
            TokenType.GreaterThan => EvaluateExpression(binaryExpression.left) > EvaluateExpression(binaryExpression.right),
            TokenType.GreaterThanOrEqual => EvaluateExpression(binaryExpression.left) >= EvaluateExpression(binaryExpression.right),
            TokenType.Equal => EvaluateExpression(binaryExpression.left) == EvaluateExpression(binaryExpression.right),
            TokenType.NotEqual => EvaluateExpression(binaryExpression.left) != EvaluateExpression(binaryExpression.right),
            TokenType.BitwiseAnd => EvaluateExpression(binaryExpression.left) & EvaluateExpression(binaryExpression.right),
            TokenType.BitwiseOr => EvaluateExpression(binaryExpression.left) | EvaluateExpression(binaryExpression.right),
            TokenType.BitwiseXor => EvaluateExpression(binaryExpression.left) ^ EvaluateExpression(binaryExpression.right),
            TokenType.ShiftLeft => EvaluateExpression(binaryExpression.left) << EvaluateExpression(binaryExpression.right),
            TokenType.ShiftRight => EvaluateExpression(binaryExpression.left) >> EvaluateExpression(binaryExpression.right),
            TokenType.Plus => EvaluateExpression(binaryExpression.left) + EvaluateExpression(binaryExpression.right),
            TokenType.Minus => EvaluateExpression(binaryExpression.left) - EvaluateExpression(binaryExpression.right),
            TokenType.Multiply => EvaluateExpression(binaryExpression.left) * EvaluateExpression(binaryExpression.right),
            TokenType.Power => Math.Pow(EvaluateExpression(binaryExpression.left), EvaluateExpression(binaryExpression.right)),
            TokenType.Divide => EvaluateExpression(binaryExpression.left) / EvaluateExpression(binaryExpression.right),
            TokenType.Modulo => EvaluateExpression(binaryExpression.left) % EvaluateExpression(binaryExpression.right),
            _ => throw new NotSupportedException($"{binaryExpression.@operator.Type.ToSourceString()} is not a supported binary operator"),
        };
    }

    private Value EvaluateUnary(UnaryExpressionNode unary) {
        Value value = EvaluateExpression(unary.expression);

        throw unary.@operator.Type switch {
            TokenType.Increment => value++,
            TokenType.Decrement => value--,
            TokenType.Not => !value,
            TokenType.BitwiseNot => ~value,
            TokenType.Plus => +value,
            TokenType.Minus => -value,
            _ => new NotSupportedException($"{unary.@operator.Type.ToSourceString()} is not a supported unary operator"),
        };
    }

    private Value EvaluateExpression(ExpressionNode expression) {
        if (expression.IsNullOrEmpty())
            return null;

        return expression switch {
            ArrayLiteralExpressionNode arrayLiteral => EvaluateArrayLiteral(arrayLiteral),
            AssignmentExpressionNode assignment => EvaluateAssignment(assignment),
            BinaryExpressionNode binary => EvaluateBinary(binary),
            CallExpressionNode call => EvaluateExpression(call.method)(call.arguments.Map(EvaluateExpression).ToArray()),
            ClosureExpressionNode closure => new Func<Value[], Value>(args => {
                currentScope = new(currentScope);
                Value result = null;
                if (!closure.isLastParams)
                    for (int i = 0; i < closure.arguments.Length; i++)
                        currentScope[closure.arguments[i].name] = args[i];
                else {
                    for (int i = 0; i < closure.arguments.Length - 1; i++)
                        currentScope[closure.arguments[i].name] = args[i];

                    currentScope[closure.arguments[^1].name] = args.Slice(new(closure.arguments.Length - 1, args.Length - 1));
                }

                currentScope = currentScope.parent;

                // cannot use evaluate block because a new scope is already declared
                if (closure.body is BlockStatementNode block) {
                    foreach (StatementNode statement in block.body) {
                        switch (EvaluateStatement(statement).Case) {
                            case ErrorType.NoError:
                                break;
                            case ErrorType error:
                                return error;
                            case object o:
                                return o;
                        }
                    }
                }

                return result;
            }),
            ComputedMemberAccessExpressionNode computedMemberAccess => EvaluateExpression(computedMemberAccess.member).GetProperty(EvaluateExpression(computedMemberAccess.member)),
            DoubleExpressionNode @double => @double.value,
            IdentifierExpressionNode identifier => currentScope[identifier.value],
            IntExpressionNode @int => @int.value,
            MemberAccessExpressionNode memberAccess => EvaluateExpression(new ComputedMemberAccessExpressionNode(memberAccess.member, new StringExpressionNode(memberAccess.property))),
            UnaryExpressionNode unary => EvaluateUnary(unary),
            RangeLiteralExpressionNode rangeLiteral => throw new NotImplementedException(),
            StringExpressionNode @string => @string.value,
            TernaryExpressionNode ternary => EvaluateExpression(ternary.condition) ? EvaluateExpression(ternary.@true) : EvaluateExpression(ternary.@false),
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
                case object o:
                    return o;
            }
        }

        currentScope = currentScope.parent;

        return ErrorType.NoError;
    }

    private ErrorType EvaluateCommand(CommandStatementNode command) {
        throw new NotImplementedException();
    }

    private Result EvaluateIf(IfStatementNode @if) {
        return EvaluateStatement(EvaluateExpression(@if.condition) ? @if.@true : @if.@false);
    }

    private Result EvaluateForEach(ForeachLoopStatementNode foreachLoop) {
        currentScope[foreachLoop.value.name] = null;
        foreach (Value it in EvaluateExpression(foreachLoop.iterable)) {
            currentScope[foreachLoop.value.name] = it;

            switch (EvaluateStatement(foreachLoop.body).Case) {
                case ErrorType.NoError:
                    break;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType.ContinueLoop:
                    continue;
                case ErrorType error:
                    return error;
                case object o:
                    return o;
            }
        }

        return ErrorType.NoError;
    }

    private Result EvaluateFor(ForLoopStatementNode forLoop) {
        if (!forLoop.init.IsNullOrEmpty())
            DeclareVariable(forLoop.init);

        for (; EvaluateExpression(forLoop.condition) ?? true; EvaluateExpression(forLoop.update)) {
            switch (EvaluateStatement(forLoop.body).Case) {
                case ErrorType.NoError:
                    break;
                case ErrorType.ContinueLoop:
                    continue;
                case ErrorType.BreakLoop:
                    return ErrorType.NoError;
                case ErrorType error:
                    return error;
                case object o:
                    return o;
            }
        }

        return ErrorType.NoError;
    }

    private Result EvaluateWhile(WhileLoopStatementNode whileLoop) {
        while (true) {
            if (EvaluateExpression(whileLoop.condition) ?? true)
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
                case object o:
                    return o;
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
        currentScope[variableDeclaration.name] = EvaluateExpression(variableDeclaration.valueExpression);
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
}