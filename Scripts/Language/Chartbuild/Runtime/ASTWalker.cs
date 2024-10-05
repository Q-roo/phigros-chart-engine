using System;
using System.Diagnostics;
using System.Linq;
using DotNext.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

// TODO: dynamic is really hurting the preformace. Create a new wrapper object
using Value = Object;
using Result = Either<Object, ErrorType>;

public class ASTWalker {
    private readonly ASTRoot ast;
    private readonly Scope rootScope;
    private Scope currentScope;
    private bool isInFunction;

    public ASTWalker(ASTRoot ast) {
        this.ast = ast;
        rootScope = new(null);
        currentScope = rootScope;
    }

    public ASTWalker InsertValue(bool @readonly, object key, Value value) {
        currentScope.DeclareVariable(key, value, @readonly);
        return this;
    }

    public void Evaluate() {
        EvaluateRoot();
    }

    private void EvaluateRoot() {
        EvaluateBlock(ast);
    }

    private Array EvaluateArrayLiteral(ArrayLiteralExpressionNode arrayLiteral) => new(arrayLiteral.content.Map(EvaluateExpression));

    private Value EvaluateAssignment(AssignmentExpressionNode assignment) {
        Value asignee = EvaluateExpression(assignment.asignee);
        Value oldValue = asignee.Copy();

        asignee.SetValue(
            assignment.@operator.Type switch {
                TokenType.DotAssign => EvaluateExpression(
                    new CallExpressionNode(
                        new ComputedMemberAccessExpressionNode(assignment.asignee, ((CallExpressionNode)assignment.value).method),
                        ((CallExpressionNode)assignment.value).arguments
                    )
                ),
                TokenType.Assign => EvaluateExpression(assignment.value),
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
                }), assignment.value))
            }
        );

        return oldValue;
    }

    private Value EvaluateBinary(BinaryExpressionNode binary) => EvaluateExpression(binary.left).ExecuteBinary(binary.@operator.Type.ToOperator(), EvaluateExpression(binary.right));

    private Value EvaluateUnary(UnaryExpressionNode unary) => EvaluateExpression(unary.expression).ExecuteUnary(unary.@operator.Type.ToOperator(), unary.prefix);

    private Value EvaluateExpression(ExpressionNode expression) {
        if (expression.IsNullOrEmpty())
            return null;

        return expression switch {
            ArrayLiteralExpressionNode arrayLiteral => EvaluateArrayLiteral(arrayLiteral),
            AssignmentExpressionNode assignment => EvaluateAssignment(assignment),
            BinaryExpressionNode binary => EvaluateBinary(binary),
            CallExpressionNode call => EvaluateExpression(call.method).Call(call.arguments.Map(it => EvaluateExpression(it).Copy()).ToArray()),
            // the scope has to be reconstructed for each call
            // which is done by the ast closure object
            ClosureExpressionNode closure => new Closure(currentScope, closure, this),
            ComputedMemberAccessExpressionNode computedMemberAccess => EvaluateExpression(computedMemberAccess.member)[EvaluateExpression(computedMemberAccess.property).Value],
            DoubleExpressionNode @double => new F32((float)@double.value),
            IdentifierExpressionNode identifier => currentScope[identifier.value],
            IntExpressionNode @int => new I32(@int.value),
            MemberAccessExpressionNode memberAccess => EvaluateExpression(new ComputedMemberAccessExpressionNode(memberAccess.member, new StringExpressionNode(memberAccess.property))),
            UnaryExpressionNode unary => EvaluateUnary(unary),
            RangeLiteralExpressionNode rangeLiteral => throw new NotImplementedException(),
            StringExpressionNode @string => new Str(@string.value),
            TernaryExpressionNode ternary => EvaluateExpression(ternary.condition).ToBool().value ? EvaluateExpression(ternary.@true) : EvaluateExpression(ternary.@false),
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
        // TODO: #set aspect_ratio=f32
        // TODO: #set default_judgeline_width=f32
        // NOTE: #meta allows values to be defined only once
        // but it should be fine to let users redefine these
        // just imagine changing the aspect ratio for a gimmick
        throw new NotImplementedException();
    }

    private Result EvaluateIf(IfStatementNode @if) {
        return EvaluateStatement(EvaluateExpression(@if.condition).ToBool().value ? @if.@true : @if.@false);
    }

    private Result EvaluateForEach(ForeachLoopStatementNode foreachLoop) {
        currentScope[foreachLoop.value.name] = null;
        foreach (Value it in EvaluateExpression(foreachLoop.iterable).ToArray().content) {
            currentScope[foreachLoop.value.name] = it.Copy();

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

        for (; forLoop.condition.IsNullOrEmpty() || EvaluateExpression(forLoop.condition).ToBool().value; EvaluateExpression(forLoop.update)) {
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
            if (whileLoop.condition.IsNullOrEmpty() || EvaluateExpression(whileLoop.condition).ToBool().value)
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
                new ClosureExpressionNode(functionDeclaration.arguments, functionDeclaration.body, functionDeclaration.isLastParams),
                true
            )
        );
    }

    private ErrorType DeclareVariable(VariableDeclarationStatementNode variableDeclaration) {
        currentScope.DeclareVariable(variableDeclaration.name, EvaluateExpression(variableDeclaration.valueExpression), variableDeclaration.@readonly);
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

    public Value CallUserDefinedClosure(Scope scope, ClosureExpressionNode closure, params Value[] args) {
        // declarations
        Scope _scope = currentScope;
        Value result = new Unset();

        // temporary function scope
        currentScope = scope;
        isInFunction = true;

        if (!closure.isLastParams)
            for (int i = 0; i < closure.arguments.Length; i++)
                scope.DeclareVariable(closure.arguments[i].name, args[i], false);
        else {
            for (int i = 0; i < closure.arguments.Length - 1; i++)
                scope.DeclareVariable(closure.arguments[i].name, args[i], false);

            scope.DeclareVariable(closure.arguments[^1].name, new Array(args.Slice(new(closure.arguments.Length - 1, args.Length - 1))), false);
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
                        currentScope = _scope;
                        isInFunction = false;
                        return o;
                    default:
                        throw new UnreachableException();
                }
            }
        }

        // switch back to the previous scope
        currentScope = _scope;
        isInFunction = false;

        return result;
    }
}