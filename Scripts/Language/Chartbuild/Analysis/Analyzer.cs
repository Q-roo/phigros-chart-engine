using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// using System.Linq;
// using DotNext;
// using Godot;
using LanguageExt;
using PCE.Chartbuild.Runtime;

namespace PCE.Chartbuild;

// TODO
/*
 * [ ] evaluate constant expressions: 1+1 AKA constant folding
 * [ ] infer type
 * [ ] infer return type
 * [ ] infer pure
 * [ ] delete pure functions with unused results
 * [ ] dead code
 * [ ] empty statemet warning
 * [ ] infinite loop waring
 * [ ] unknown type error
 * [ ] invalid type error
 * [ ] inline for/foreach loops with known cycles
 * [ ] file does not start with #version error
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
                    switch (AnalyzeExpression(expression.expression, block.scope).Case) {

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
                    } else if (i < block.body.Count - 1)
                        block.body.RemoveRange(i + 1, block.body.Count - i);
                    break;
                case ReturnStatementNode:
                    if (!isFunctionBody) {
                        errors.Add(new(ErrorType.UnexpectedToken, "return statements can only be used inside functions", -1, -1));
                        block.body.RemoveAt(i);
                        i--;
                    } else if (i < block.body.Count - 1)
                        block.body.RemoveRange(i + 1, block.body.Count - i);
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

    // TODO: invalid return type
    private static DeclaredFunction AnalyzeFunctionDeclaration(List<Error> errors, FunctionDeclarationStatementNode functionDeclaration, Scope scope, bool isLoopBody) {
        functionDeclaration.body.scope.parent = scope;
        AnalyzeBlockLike(errors, functionDeclaration.body, functionDeclaration.body.scope, isLoopBody, true);

        foreach (FunctionParameter parameter in functionDeclaration.arguments)
            if (functionDeclaration.body.scope.DeclareVariable(parameter.name, new NullValue(), false) != ErrorType.NoError)
                throw new Exception("couldn't declare function parameters");

        return new DeclaredFunction() {
            returnType = functionDeclaration.returnType ?? new NullValue().Type,
            argumentTypes = [],
            isLastParams = functionDeclaration.arguments[^1].type.@params, // TODO: range test, testing only
            pure = true, // testing
            argumentNames = ["a", "b"], // testing
            body = functionDeclaration.body
        };
    }

    private static Either<ExpressionStatementNode, ErrorType> AnalyzeExpression(ExpressionNode expression, Scope scope) {
        Godot.GD.Print(expression.Evaluate(scope).MapLeft(v => v.GetValue()));
        throw new NotImplementedException("TODO");
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

    private static Either<StatementNode, ErrorType> AnalyzeWhileLoop(List<Error> errors, WhileLoopStatementNode whileLoop, Scope scope, bool isFunctionBody) {
        AnalyzeLoopBody(errors, whileLoop, scope, isFunctionBody);

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

        // loop body is empty, just return init and condition because those would've ran at least once
        if (forLoop.body.body.Count == 0) {
            if (forLoop.init is not null)
                forLoop.body.body.Add(forLoop.init);
            if (forLoop.update is not null)
                switch (AnalyzeExpression(forLoop.update, forLoop.body.scope).Case) {
                    case ExpressionStatementNode expression:
                        forLoop.body.body.Add(expression);
                        return forLoop.body;
                    case ErrorType err:
                        return err;
                    default:
                        throw new UnreachableException();
                };
        }

        if (forLoop.condition is not null) {
            switch (AnalyzeExpression(forLoop.condition, forLoop.body.scope).Case) {
                case ExpressionStatementNode expression:
                    forLoop.condition = expression.expression;
                    break;
                case ErrorType err:
                    if (err != ErrorType.NoError || err != ErrorType.NotCompileTimeConstant)
                        return err;
                    break;
                default:
                    throw new UnreachableException();
            }
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

        if (forLoop.update is not null)
            switch (AnalyzeExpression(forLoop.update, forLoop.body.scope).Case) {
                case ExpressionStatementNode expression:
                    forLoop.update = expression.expression;
                    break;
                case ErrorType err:
                    if (err != ErrorType.NoError || err != ErrorType.NotCompileTimeConstant)
                        return err;
                    break;
                default:
                    throw new UnreachableException();
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

        return forEachLoop.iterable.Evaluate(scope).Case switch {
            ArrayValue => forEachLoop,
            ICBValue => ErrorType.InvalidType,
            ErrorType err => err,
            _ => throw new UnreachableException()
        };
    }
}