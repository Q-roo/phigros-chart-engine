using System;
using System.Collections.Generic;
using System.Diagnostics;
using DotNext.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild.Runtime;

// TODO: dynamic is really hurting the preformace. Create a new wrapper object
using Value = Object;
using Result = Either<Object, ErrorType>;

public class ASTWalker {
    private readonly ASTRoot ast;
    private readonly Scope rootScope;
    private Scope _currentScope;
    public Scope CurrentScope {
        get => _currentScope;
        set {
            _currentScope = value;
            // in case a child scope sets it, roll it back
            _currentScope.rules.UpdateAspectRatio();
        }
    }
    private bool isInFunction;

    public ASTWalker(ASTRoot ast) {
        this.ast = ast;
        rootScope = new();
        CurrentScope = rootScope;
    }

    public ASTWalker InsertValue(bool @readonly, object key, Value value) {
        CurrentScope.DeclareVariable(key, value, @readonly);
        return this;
    }

    public ASTWalker InsertProperty(object key, Property getter) {
        CurrentScope.AddProperty(key, getter);
        return this;
    }

    public ASTWalker InsertProperty(object key, Getter<object> getter) {
        return InsertProperty(key, new ReadOnlyProperty(CurrentScope, key, getter));
    }

    public delegate Value Getter();
    public ASTWalker InsertProperty(object key, Getter getter) {
        return InsertProperty(key, (_, _) => getter());
    }

    public void Evaluate() {
        EvaluateRoot();
    }

    private void EvaluateRoot() {
        EvaluateBlock(ast);
    }

    private Array EvaluateArrayLiteral(ArrayLiteralExpressionNode arrayLiteral) => new(arrayLiteral.content.Map(it => EvaluateExpression(it).GetValue()));

    private Value EvaluateAssignment(AssignmentExpressionNode assignment) {
        Property asignee = EvaluateExpression(assignment.asignee) is Property property ? property : throw new Exception("this should be a property");
        Value oldValue = asignee.Get().Copy();

        asignee.Set(
            assignment.@operator.Type switch {
                TokenType.DotAssign => EvaluateExpression(
                    new CallExpressionNode(
                        new ComputedMemberAccessExpressionNode(assignment.asignee, ((CallExpressionNode)assignment.value).method),
                        ((CallExpressionNode)assignment.value).arguments
                    )
                ),
                TokenType.Assign => EvaluateExpression(assignment.value).GetValue(),
                TokenType token => EvaluateBinary(new(new ValueExpressionNode<Value>(asignee.Get()), new Token(-1, -1, token switch {
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

    private Value EvaluateBinary(BinaryExpressionNode binary) => EvaluateExpression(binary.left).GetValue().BinaryOperation(binary.@operator.Type.ToOperator(), EvaluateExpression(binary.right).GetValue());

    private Value EvaluateUnary(UnaryExpressionNode unary) => EvaluateExpression(unary.expression).GetValue().UnaryOperation(unary.@operator.Type.ToOperator(), unary.prefix);

    private Value EvaluateExpression(ExpressionNode expression) {
        if (expression.IsNullOrEmpty())
            return null;

        return expression switch {
            ArrayLiteralExpressionNode arrayLiteral => EvaluateArrayLiteral(arrayLiteral),
            AssignmentExpressionNode assignment => EvaluateAssignment(assignment),
            BinaryExpressionNode binary => EvaluateBinary(binary),
            CallExpressionNode call => EvaluateExpression(call.method).GetValue().Call([.. call.arguments.Map(it => EvaluateExpression(it).GetValue().Copy())]),
            // the scope has to be reconstructed for each call
            // which is done by the ast closure object
            ClosureExpressionNode closure => new Closure(CurrentScope, closure, this),
            ComputedMemberAccessExpressionNode computedMemberAccess => EvaluateExpression(computedMemberAccess.member).GetValue().GetProperty(EvaluateExpression(computedMemberAccess.property).GetValue().NativeValue),
            DoubleExpressionNode @double => (float)@double.value,
            IdentifierExpressionNode identifier => CurrentScope.GetProperty(identifier.value),
            IntExpressionNode @int => @int.value,
            MemberAccessExpressionNode memberAccess => EvaluateExpression(new ComputedMemberAccessExpressionNode(memberAccess.member, new StringExpressionNode(memberAccess.property))),
            UnaryExpressionNode unary => EvaluateUnary(unary),
            RangeLiteralExpressionNode rangeLiteral => throw new NotImplementedException(),
            StringExpressionNode @string => @string.value,
            TernaryExpressionNode ternary => EvaluateExpression(ternary.condition).GetValue() ? EvaluateExpression(ternary.@true) : EvaluateExpression(ternary.@false),
            ValueExpressionNode<Value> value => value.value,
            _ => throw new NotSupportedException($"evaluation of {expression.GetType()} is not supported")
        };
    }

    private Result EvaluateBlock(BlockStatementNode block) {
        CurrentScope = new(CurrentScope);

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

        CurrentScope = CurrentScope.parent;

        return ErrorType.NoError;
    }

    private ErrorType EvaluateCommand(CommandStatementNode command) {
        // NOTE: unlike #set, #meta allows values to be defined only once
        // but it should be fine to let users redefine these
        // just imagine changing the aspect ratio for a gimmick
        switch (command.name) {
            case "set":
                if (
                    command.expression is not AssignmentExpressionNode assignment
                    || assignment.asignee is not IdentifierExpressionNode identifier
                    || assignment.@operator.Type != TokenType.Assign
                )
                    throw new InvalidOperationException("set commands must be assignment expressions with identifiers as asignees and use \"=\" for asignment");
                switch (identifier.value) {
                    case "aspect_ratio":
                        CurrentScope.rules.AspectRatio = EvaluateExpression(assignment.value);
                        break;
                    case "default_judgeline_width":
                        CurrentScope.rules.DefaultJudgelineSize = EvaluateExpression(assignment.value);
                        break;
                    case "default_judgeline_bpm":
                        CurrentScope.rules.DefaultJudgelineBpm = EvaluateExpression(assignment.value);
                        break;
                    case "default_note_speed":
                        CurrentScope.rules.DefaultNoteSpeed = EvaluateExpression(assignment.value);
                        break;
                    case "default_note_is_above":
                        CurrentScope.rules.DefaultIsNoteAbove = EvaluateExpression(assignment.value);
                        break;
                    default:
                        throw new KeyNotFoundException($"there is no value with the name \"{identifier.value}\"");
                }
                break;
            case "version":
            case "meta":
            case "enable":
            case "disable":
                throw new NotImplementedException();
            default:
                throw new InvalidOperationException($"unknown command: {command.name}");

        }

        return ErrorType.NoError;
    }

    private Result EvaluateIf(IfStatementNode @if) {
        return EvaluateStatement(EvaluateExpression(@if.condition) ? @if.@true : @if.@false);
    }

    private Result EvaluateForEach(ForeachLoopStatementNode foreachLoop) {
        CurrentScope.AddProperty(foreachLoop.value.name, new ValueProperty(CurrentScope, foreachLoop.value.name, null));
        foreach (Value it in EvaluateExpression(foreachLoop.iterable).ToList()) {
            CurrentScope.GetProperty(foreachLoop.value.name).Set(it.Copy());

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

        for (; forLoop.condition.IsNullOrEmpty() || EvaluateExpression(forLoop.condition); EvaluateExpression(forLoop.update)) {
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
            if (whileLoop.condition.IsNullOrEmpty() || EvaluateExpression(whileLoop.condition))
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
        CurrentScope.DeclareVariable(variableDeclaration.name, EvaluateExpression(variableDeclaration.valueExpression), variableDeclaration.@readonly);
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
        Scope _scope = CurrentScope;
        Value result = new Unset();

        // temporary function scope
        CurrentScope = scope;
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
                    case Value o:
                        CurrentScope = _scope;
                        isInFunction = false;
                        return o;
                    default:
                        throw new UnreachableException();
                }
            }
        } else {
            switch (EvaluateStatement(closure.body).Case) {
                case ErrorType.NoError:
                    break;
                case Value o:
                    CurrentScope = _scope;
                    isInFunction = false;
                    return o;
                default:
                    throw new UnreachableException();
            }
        }

        // switch back to the previous scope
        CurrentScope = _scope;
        isInFunction = false;

        return result;
    }
}