using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// TODO: type checking (IMPORTANT)

// TODO
/*
 * [ ] infer type
 * [x] infer pure (hopefully, it works)
 * [x] dead code
 * [ ] empty statemet warning
 * [ ] infinite loop waring
 * [ ] invalid type error
 * [x] file does not start with #version error
 * [ ] invalid version error
*/

// pure function
/*
 * all parameters are pure
 * finction calls might be pure or not
 * return value's type is identical to the parameter's
 * no assignments
 * no loops
 * "a function is pure if the only result it produces is it's return value"
 * no references dereferences on the left side of an operator (e.g.: *a = 0; is impure)
*/

/* pure function examples
 * sum(a,b) => a+b;
 * sumexp2(a,b) => 2 ** sum(a,b);
 * ret1() => 1;
*/

// dead code
/*
 * return;
 * code here
*/
/*
 * while(true) or something similar
 * no break
  * code here
*/

// code that will never run
/*
 * if (false)
*/
/*
 * if (true)
 * else
*/

// functions with empty bodies / return none should be skipped as well
// error: return statement outside of function
// error: break or continue outside of loop

// lots of optimizations: https://youtu.be/QSPaL4aVjRo?si=9wM5ARghqaEiKoJL&t=539

public class Analyzer {
    // FIXME: current limitation
    /*
     * fn p(){print(a)} <- error: missing member "a"
     * const a = 0;
    */
    public static ASTRoot Analyze(ASTRoot ast) {
        ast.scope.DeclareVariable("true", new BoolValue(true), true);
        ast.scope.DeclareVariable("false", new BoolValue(false), true);
        ast.scope.DeclareVariable("unset", new NullValue(), true); // I can already feel the bugs this will cause

        if (ast.body.Count > 0)
            if (ast.body[0] is CommandStatementNode command)
                if (command.name == "version") {
                    AnalyzeBlockLike(ast.errors, ast, ast.scope, false, false);
                    return ast;
                }

        ast.errors.Add(new(ErrorType.DoesNotStartWithVersion, "chart build scripts must start with defining a version", -1, -1));
        return ast;
    }

    private static T AnalyzeBlockLike<T>(List<Error> errors, T block, Scope parentScope, bool isLoopBody, bool isFunctionBody) where T : BlockStatementNode {
        // top level variable declarations
        // create the variables and remove the statements
        for (int i = 0; i < block.body.Count; i++) {
            switch (block.body[i]) {
                case BlockStatementNode blockNode: {
                    blockNode.scope.parent = parentScope;
                    AnalyzeBlockLike(errors, blockNode, block.scope, isLoopBody, isFunctionBody);
                    if (blockNode.body.Count == 0) { // maybe add a warning
                        block.body.RemoveAt(i);
                        i--;
                    }
                    break;
                }
                case EmptyStatementNode: // maybe add a warning
                    {
                    block.body.RemoveAt(i);
                    i--;
                    break;
                }
                case CommandStatementNode command: {
                    ErrorType error = AnalyzeCommand(command);
                    if (error != ErrorType.NoError)
                        errors.Add(new(error, error.ToString(), -1, -1));

                    // commands are meant to be compile time only
                    block.body.RemoveAt(i);
                    i--;
                    break;
                }
                case VariableDeclarationStatementNode variableDeclaration: {
                    ErrorType error = AnalyzeVariableDeclaration(errors, block.scope, variableDeclaration);

                    if (error == ErrorType.NotCompileTimeConstant) {
                        // replace declaration with assignment
                        // and declare the variable
                        // reassign error in case this causes a duplicate identifier error
                        error = block.scope.DeclareNonConstant(variableDeclaration.name, variableDeclaration.@readonly, variableDeclaration.valueExpression is not null);

                        // the analyzer will go over the ast only once
                        // so this won't be a problem
                        block.body[i] = new ExpressionStatementNode(
                            new AssignmentExpressionNode(
                                new IdentifierExpressionNode(variableDeclaration.name),
                                new Token(-1, -1, TokenType.Assign),
                                variableDeclaration.valueExpression
                            )
                        );
                    } else {
                        block.body.RemoveAt(i);
                        i--;
                    }

                    if (error == ErrorType.InvalidType)
                        errors.Add(new(error, $"wrong type for {variableDeclaration.name}", -1, -1));

                    if (error == ErrorType.DuplicateIdentifier)
                        errors.Add(new(error, $"{variableDeclaration.name} is already defined", -1, -1));
                    break;
                }
                case FunctionDeclarationStatementNode functionDeclaration: {
                    ErrorType error = block.scope.DeclareVariable(functionDeclaration.name, new CBFunctionValue(AnalyzeFunctionDeclaration(errors, functionDeclaration, block.scope, isLoopBody)), true);
                    if (error != ErrorType.NoError)
                        errors.Add(new(error, error.ToString(), -1, -1));

                    block.body.RemoveAt(i);
                    i--;
                    break;
                }
                case ExpressionStatementNode expression: {
                    expression.expression = AnalyzeExpression(expression.expression, block.scope);
                    // remove expressions that don't do anything such as 1+2 or 1 + add(1,2);
                    // leave it if there are non-pure function calls in the expression
                    // leave assignment and prefix and postfix expressions
                    switch (expression.expression) {
                        case AssignmentExpressionNode or UnaryExpressionNode:
                            break;
                        default:
                            switch (expression.Evaluate(block.scope).Case) {
                                case not ErrorType.NotCompileTimeConstant:
                                    block.body.RemoveAt(i);
                                    i--;
                                    break;
                            }
                            break;
                    }
                    break;
                }
                case IfStatementNode @if: {
                    switch (AnalyzeIf(errors, @if, block.scope, isLoopBody, isFunctionBody).Case) {
                        case EmptyStatementNode:
                            block.body.RemoveAt(i);
                            i--;
                            break;
                        case StatementNode statement:
                            block.body[i] = statement;
                            break;
                        case ErrorType err:
                            errors.Add(new(err, err.ToString(), -1, -1));
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    break;
                }
                case WhileLoopStatementNode whileLoop: {
                    switch (AnalyzeWhileLoop(errors, whileLoop, block.scope, isFunctionBody).Case) {
                        case EmptyStatementNode:
                            block.body.RemoveAt(i);
                            i--;
                            break;
                        case ErrorType err when err is not ErrorType.NotCompileTimeConstant:
                            errors.Add(new(err, err.ToString(), -1, -1));
                            break;
                    }
                    break;
                }
                case ForLoopStatementNode forLoop: {
                    switch (AnalyzeForLoop(errors, forLoop, block.scope, isFunctionBody).Case) {
                        case EmptyStatementNode:
                            block.body.RemoveAt(i);
                            i--;
                            break;
                        case StatementNode statement:
                            block.body[i] = statement;
                            break;
                        case ErrorType err when err is not ErrorType.NotCompileTimeConstant:
                            errors.Add(new(err, err.ToString(), -1, -1));
                            break;
                    }
                    break;
                }
                case ForeachLoopStatementNode foreachLoop: {
                    switch (AnalyzeForeachLoop(errors, foreachLoop, block.scope, isFunctionBody).Case) {
                        case EmptyStatementNode:
                            block.body.RemoveAt(i);
                            i--;
                            break;
                        case ForeachLoopStatementNode: // the loop reference has been modified in the function, no need to reassign it
                            break;
                        case ErrorType err:
                            errors.Add(new(err, err.ToString(), -1, -1));
                            break;
                    }
                    break;
                }
                case BreakStatementNode or ContinueStatementNode:
                    if (!isLoopBody) {
                        errors.Add(new(ErrorType.UnexpectedToken, "break and continue statements can only be used inside loops", -1, -1));
                        block.body.RemoveAt(i);
                        i--;
                    } else
                        block.body.RemoveRange(i + 1, block.body.Count - (i + 1));
                    break;
                case ReturnStatementNode:
                    if (!isFunctionBody) {
                        errors.Add(new(ErrorType.UnexpectedToken, "return statements can only be used inside functions", -1, -1));
                        block.body.RemoveAt(i);
                        i--;
                    } else
                        block.body.RemoveRange(i + 1, block.body.Count - (i + 1));
                    break;
                default:
                    break;
            }
        }

        return block;
    }

    private static ErrorType AnalyzeCommand(CommandStatementNode command) {
        return command.name switch {
            // TODO
            "version" => ErrorType.NotSupported, // versions will define the available commands, features, ..etc
            "target" => ErrorType.NotSupported, // which platform the script targets (phigros, chart engine or phira). it will enable or disable features and such to ensure compatibility
            "enable" => ErrorType.NotSupported, // enable a feature
            "disable" => ErrorType.NotSupported, // disable a feature
            _ => ErrorType.InvalidCommand,
        };
    }

    private static ErrorType AnalyzeVariableDeclaration(List<Error> errors, Scope scope, VariableDeclarationStatementNode variableDeclaration) {
        ICBValue value = null;
        if (variableDeclaration.valueExpression is not null)
            switch (variableDeclaration.valueExpression.Evaluate(scope).Case) {
                case ICBValue v:
                    value = v;
                    if (variableDeclaration.type is not null && variableDeclaration.type != v.Type) {
                        return ErrorType.InvalidType;
                    }
                    break;
                case ErrorType err:
                    if (err != ErrorType.NotCompileTimeConstant)
                        errors.Add(new(err, err.ToString(), -1, -1));

                    break;
                default:
                    throw new UnreachableException();
            }

        return scope.DeclareVariable(
            variableDeclaration.name,
            value,
            variableDeclaration.@readonly
        );
    }

    private static DeclaredFunction AnalyzeFunctionDeclaration(List<Error> errors, FunctionDeclarationStatementNode functionDeclaration, Scope scope, bool isLoopBody) {
        functionDeclaration.body.scope.parent = scope;
        AnalyzeBlockLike(errors, functionDeclaration.body, functionDeclaration.body.scope, isLoopBody, true);

        foreach (FunctionParameter parameter in functionDeclaration.arguments)
            if (functionDeclaration.body.scope.DeclareVariable(parameter.name, new NullValue(), false) != ErrorType.NoError)
                throw new Exception("couldn't declare function parameters");

        BaseType[] types = functionDeclaration.arguments.Select(it => it.type).ToArray();
        string[] paramNames = functionDeclaration.arguments.Select(it => it.name).ToArray();


        if (functionDeclaration.isLastParams)
            types[^1] = new ArrayType(types[^1]);

        return new DeclaredFunction() {
            returnType = functionDeclaration.returnType ?? new NullValue().Type,
            argumentTypes = types,
            isLastParams = functionDeclaration.isLastParams,
            // FIXME: the evaluation will try using the function params which are set to null
            pure = functionDeclaration.body.Evaluate(functionDeclaration.body.scope).Case switch {
                ErrorType.NoError or ErrorType.NullValue => true, // there should be only 4+1 cases
                ErrorType => false,                               // 1: null value error (the function parameter values are set to null at this point)
                                                                  // 2: no error but no value to return
                                                                  // 3: no error and there is a value to return
                                                                  // 4: not compile time constant AKA, not pure
                                                                  // 5: invalid syntax, types and such in which case, the code won't even run
                _ => true
            },
            argumentNames = paramNames,
            body = functionDeclaration.body
        };
    }

    // optimizes the expression and returns errors if there are any
    // it mutates the expression inside the function but still returns it
    // it mostly does constant folding
    private static ExpressionNode AnalyzeExpression(ExpressionNode expression, Scope scope) {
        // Godot.GD.Print(expression.Evaluate(scope).MapLeft(v => v.GetValue()));
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
                switch (binary.Evaluate(scope).Case) {
                    case ICBValue value:
                        return new ValueExpressionNode<ICBValue>(value);
                }
                break;
            case CallExpressionNode call:
                switch (call.Evaluate(scope).Case) {
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
                switch (ternary.condition.Evaluate(scope).Case) {
                    case BoolValue @bool:
                        return @bool ? ternary.@true : ternary.@false;
                }
                break;
            // the rest are literals
            default:
                break;
        }
        // throw new NotImplementedException("TODO");
        return expression;
    }

    // if (false){}
    // else {...}
    // return the else block if it's not empty else empty
    // if (?){}
    // else {...}
    // return if(!?){...}
    // if(?){}
    // else {}
    // return ? as a statement
    private static Either<StatementNode, ErrorType> AnalyzeIf(List<Error> errors, IfStatementNode @if, Scope scope, bool isLoopBody, bool isFunctionBody) {
        bool trueEmpty = @if.@true is null or EmptyStatementNode;
        bool falseEmpty = @if.@false is null or EmptyStatementNode;

        if (@if.@true is BlockStatementNode @true) {
            @true.scope.parent = scope;
            AnalyzeBlockLike(errors, @true, @true.scope, isLoopBody, isFunctionBody);
            trueEmpty = @true.body.Count == 0;
        }

        if (@if.@false is BlockStatementNode @false) {
            @false.scope.parent = scope;
            AnalyzeBlockLike(errors, @false, @false.scope, isLoopBody, isFunctionBody);
            trueEmpty = @false.body.Count == 0;
        }

        switch (@if.condition.Evaluate(scope).Case) {
            case ICBValue value:
                switch (value) {
                    case BoolValue @bool:

                        // if (true);
                        if (@bool)
                            return trueEmpty ? new EmptyStatementNode() : @if.@true;

                        // if (false){...}
                        // else;
                        if (!@bool)
                            return falseEmpty ? new EmptyStatementNode() : @if.@false;
                        break;
                    default:
                        return ErrorType.InvalidType;
                }
                break;
            case ErrorType err:
                if (err == ErrorType.NotCompileTimeConstant) {
                    if (trueEmpty && falseEmpty)
                        return new ExpressionStatementNode(@if.condition);
                    else if (trueEmpty) {
                        @if.@true = new EmptyStatementNode();
                        @if.condition = new PrefixExpressionNode(new Token(-1, -1, TokenType.Not), @if.condition);
                    } else if (falseEmpty)
                        @if.@false = new EmptyStatementNode();

                    return @if;
                }
                return err;
            default:
                throw new UnreachableException();
        }

        return @if;
    }

    // setting the body to empty list will make it get removed
    private static void AnalyzeLoopBody(List<Error> errors, LoopStatementNode loop, Scope scope, bool isFunctionBody) {
        loop.body.scope.parent = scope;
        AnalyzeBlockLike(errors, loop.body, loop.body.scope, true, isFunctionBody);
        if (loop.body.body.FirstOrDefault() is ReturnStatementNode or BreakStatementNode) // loop:{} or loop:{break;}
            loop.body.body.Clear();
    }

    // no need for if checks because the parser doesn't let while loops with nonexistent conditions to be created
    private static Either<StatementNode, ErrorType> AnalyzeWhileLoop(List<Error> errors, WhileLoopStatementNode whileLoop, Scope scope, bool isFunctionBody) {
        AnalyzeLoopBody(errors, whileLoop, scope, isFunctionBody);
        whileLoop.condition = AnalyzeExpression(whileLoop.condition, scope);

        // if the condition could have side effects, the loop will remain, even with an empty body
        return whileLoop.condition.Evaluate(scope).Case switch {
            BoolValue @bool => @bool && whileLoop.body.body.Count > 0 ? whileLoop : new EmptyStatementNode(),
            ICBValue => ErrorType.InvalidType,
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }

    // FIXME: for (i ;;) is not possible
    private static Either<StatementNode, ErrorType> AnalyzeForLoop(List<Error> errors, ForLoopStatementNode forLoop, Scope scope, bool isFunctionBody) {
        AnalyzeLoopBody(errors, forLoop, scope, isFunctionBody);
        if (forLoop.init is not null)
            switch (AnalyzeVariableDeclaration(errors, forLoop.body.scope, forLoop.init)) {
                case ErrorType.NoError or ErrorType.NotCompileTimeConstant:
                    break;
                case ErrorType err:
                    return err;
            }

        if (forLoop.condition is not null)
            forLoop.condition = AnalyzeExpression(forLoop.condition, scope);

        if (forLoop.update is not null)
            forLoop.update = AnalyzeExpression(forLoop.update, scope);

        // loop body is empty, just return init and condition because those would've ran at least once
        // return then only if they are not compile time constant
        if (forLoop.body.body.Count == 0) {
            if (forLoop.init is not null)
                switch (forLoop.init.Evaluate(scope).Case) {
                    case ErrorType.NotCompileTimeConstant:
                        forLoop.body.body.Add(forLoop.init);
                        break;
                }
            if (forLoop.condition is not null)
                switch (forLoop.condition.Evaluate(scope).Case) {
                    case ErrorType.NotCompileTimeConstant:
                        forLoop.body.body.Add(new ExpressionStatementNode(forLoop.condition));
                        break;
                }

            return forLoop.body.body.Count > 0 ? forLoop.body : new EmptyStatementNode();
        }

        if (forLoop.condition is not null) {
            switch (forLoop.condition.Evaluate(scope).Case) {
                case BoolValue @bool:
                    if (!@bool) // for(;false;)
                        return new EmptyStatementNode();

                    break;
                case ICBValue:
                    return ErrorType.InvalidType;
                case ErrorType err:
                    if (err == ErrorType.NoError || err == ErrorType.NotCompileTimeConstant)
                        break;
                    return err;
                default:
                    throw new UnreachableException();
            }
        }

        return forLoop;
    }

    private static Either<StatementNode, ErrorType> AnalyzeForeachLoop(List<Error> errors, ForeachLoopStatementNode forEachLoop, Scope scope, bool isFunctionBody) {
        AnalyzeLoopBody(errors, forEachLoop, scope, isFunctionBody);

        // since iterating over something without a body shouldn't have side effects,
        // the loop can be removed
        if (forEachLoop.body.body.Count == 0)
            return new EmptyStatementNode();

        ErrorType error = AnalyzeVariableDeclaration(errors, forEachLoop.body.scope, forEachLoop.value);
        if (error != ErrorType.NoError)
            return error;

        forEachLoop.iterable = AnalyzeExpression(forEachLoop.iterable, scope);

        return forEachLoop.iterable.Evaluate(scope).Case switch {
            ArrayValue => forEachLoop,
            ICBValue => ErrorType.InvalidType,
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }
}