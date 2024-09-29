using System;
using System.Collections.Generic;
using LanguageExt;

namespace PCE.Chartbuild;

public class Parser(BaseToken[] tokens) {
    public const Version ForVersion = Version.V_0;

    private readonly BaseToken[] tokens = tokens;
    private readonly List<Error> errors = [];
    private int position;

    private BaseToken CurrentToken => tokens[position];
    private TokenType CurrentType => CurrentToken.Type;
    private bool HasTokens => position < tokens.Length && CurrentType != TokenType.Eof;

    private string ErrorName => $"!!{errors.Count - 1}";

    public ASTRoot Parse() // return ABS
    {
        List<StatementNode> nodes = [];

        for (; HasTokens;)
            nodes.Add(ParseStatement());

        return new([.. nodes], errors);
    }

    // NOTE: feels really dodgy
    private object PushError(Error error) {
        errors.Add(error);

        return null;
    }

    private BaseToken Advance() {
        return tokens[position++];
    }

    private Option<BaseToken> Expect(TokenType type) {
        return Expect(type, ErrorType.UnexpectedToken, null);
    }

    public Option<BaseToken> Expect(TokenType type, ErrorType error) {
        return Expect(type, error, null);
    }

    public Option<BaseToken> Expect(TokenType type, string msg) {
        return Expect(type, ErrorType.UnexpectedToken, msg);
    }

    private Option<BaseToken> Expect(TokenType type, ErrorType error, string msg) {
        if (CurrentType == type)
            return Advance();

        msg ??= $"expected {type.ToSourceString()} but found {CurrentType.ToSourceString()}";
        PushError(new(CurrentToken, error, msg));

        return null;
    }

    public string GetIdentifierName() {
        return Expect(TokenType.Identifier).Case switch {
            IdentifierToken token => token.name,
            _ => ErrorName
        };
    }

    private StatementNode ParseStatement() {
        return Statement().Case switch {
            Option<StatementNode> node => node.Case switch {
                StatementNode statement => statement,
                _ => ParseExpressionStatement()
            },
            _ => ParseExpressionStatement()
        };
    }

    private T ParseSimpleStatementAndAdvance<T>() where T : StatementNode, new() {
        Advance();
        return new T();
    }

    private Either<Option<StatementNode>, Error> Statement() {
        return CurrentType switch {
            TokenType.Hash => Option<StatementNode>.Some(ParseCommandStatement()),

            TokenType.Let or
            TokenType.Const => Option<StatementNode>.Some(ParseVariableDeclaration()),
            TokenType.Fn => Option<StatementNode>.Some(ParseFunctionDeclaration()),

            TokenType.LeftBrace => Option<StatementNode>.Some(ParseBlockStatement()),

            TokenType.If => Option<StatementNode>.Some(ParseIfStatement()),
            TokenType.Else => new Error(CurrentToken, ErrorType.UnexpectedToken, "else cannot start a statement"),
            TokenType.While => Option<StatementNode>.Some(ParseWhileLoopStatement()),
            TokenType.For => Option<StatementNode>.Some(ParseForLoopStatement()),
            TokenType.Switch => new Error(CurrentToken, ErrorType.NotSupported, "switch statements are not implemented yet"),
            TokenType.Case => new Error(CurrentToken, ErrorType.UnexpectedToken, "case cannot start a statement"),
            TokenType.Break => Option<StatementNode>.Some(ParseSimpleStatementAndAdvance<BreakStatementNode>()),
            TokenType.Continue => Option<StatementNode>.Some(ParseSimpleStatementAndAdvance<ContinueStatementNode>()),
            TokenType.Return => Option<StatementNode>.Some(ParseReturnStatement()),

            TokenType.Semicolon => Option<StatementNode>.Some(ParseSimpleStatementAndAdvance<EmptyStatementNode>()),

            _ => Option<StatementNode>.None
        };
    }

    #region statement parsers

    private ReturnStatementNode ParseReturnStatement() {
        Advance();
        ExpressionNode @return = null;

        if (CurrentType != TokenType.Semicolon)
            @return = ParseExpression(BindingPower.Default);

        Expect(TokenType.Semicolon);

        return new(@return);
    }

    // NOTE: #identifier will result in an error, an expression must be assigned
    private CommandStatementNode ParseCommandStatement() {
        Advance();
        return new(GetIdentifierName(), ParseExpression(BindingPower.Default));
    }

    private ExpressionStatementNode ParseExpressionStatement() {
        ExpressionNode expression = ParseExpression(BindingPower.Default);
        Expect(TokenType.Semicolon);
        return new(expression);
    }

    private BlockStatementNode ParseBlockStatement() {
        Advance();
        List<StatementNode> body = [];

        while (HasTokens && CurrentType != TokenType.RightBrace)
            body.Add(ParseStatement());

        Expect(TokenType.RightBrace);

        return new([.. body]);
    }

    private VariableDeclarationStatementNode ParseVariableDeclaration() {
        // bool isReadonly = Advance().Type == TokenType.Const;
        string name = GetIdentifierName();

        ExpressionNode valueExpression = null;

        if (CurrentType == TokenType.Assign) {
            Advance();
            valueExpression = ParseExpression(BindingPower.Assignment);
        }

        // if (type is null && valueExpression is null)
        //     throw new NotImplementedException("TODO: unknown type error: cannot infer type");

        Expect(TokenType.Semicolon);

        return new(name, valueExpression);
    }

    private FunctionDeclarationStatementNode ParseFunctionDeclaration() {
        Advance();
        string fnName = GetIdentifierName();
        List<FunctionParameter> arguments = [];
        bool paramsArg = false;

        Expect(TokenType.LeftParenthesis);
        while (HasTokens && CurrentType != TokenType.RightParenthesis) {
            if (CurrentType == TokenType.DotDot) {
                paramsArg = true;
                Advance();
            }

            string argName = GetIdentifierName();

            arguments.Add(new(argName));

            if (paramsArg) {
                Expect(TokenType.RightParenthesis, "params argument must be the last argument");
                break;
            }

            if (CurrentType != TokenType.RightParenthesis)
                Expect(TokenType.Coma);
        }

        if (!paramsArg) // expect was already called if params arg is true
            Expect(TokenType.RightParenthesis);

        return new(fnName, [.. arguments], paramsArg, ParseBlockStatement());
    }

    private IfStatementNode ParseIfStatement() {
        Advance();
        Expect(TokenType.LeftParenthesis);
        ExpressionNode condition = ParseExpression(BindingPower.Default);
        Expect(TokenType.RightParenthesis);

        StatementNode @true = ParseStatement();
        StatementNode @false = null;

        if (CurrentType == TokenType.Else) {
            Advance();
            @false = ParseStatement();
        }

        return new(condition, @true, @false);
    }

    private WhileLoopStatementNode ParseWhileLoopStatement() {
        Advance();
        Expect(TokenType.LeftParenthesis);
        ExpressionNode condition = ParseExpression(BindingPower.Default);
        Expect(TokenType.RightParenthesis);

        return new(condition, ParseStatement());
    }

    private LoopStatementNode ParseForLoopStatement() {
        // TODO: for (...;a++, b++)
        // for (const it in iterable)
        // for (let a = 0; a < n; a++)
        // for (;;)

        Advance();
        Expect(TokenType.LeftParenthesis);

        if (CurrentType == TokenType.Semicolon) //for (;...)
        {
            Advance();
            ExpressionNode condition = null;
            ExpressionNode update = null;

            if (CurrentType != TokenType.Semicolon)
                condition = ParseExpressionStatement().expression;
            else
                Advance();

            if (CurrentType != TokenType.RightParenthesis) {
                update = ParseExpression(BindingPower.Default);
                Expect(TokenType.RightParenthesis);
            } else
                Advance();

            return new ForLoopStatementNode(null, condition, update, ParseStatement());
        }

        if (CurrentType != TokenType.Let && CurrentType != TokenType.Const)
            PushError(new(Advance(), ErrorType.UnexpectedToken, "variable needs const or let access modifier"));

        // bool constant = Advance().Type == TokenType.Const;
        string name = GetIdentifierName();

        if (CurrentType == TokenType.Assign) // for (i = 0; i < n; i++)
        {
            Advance();
            VariableDeclarationStatementNode init = new(name, ParseExpression(BindingPower.Assignment));
            Expect(TokenType.Semicolon);

            ExpressionNode condition = null;
            ExpressionNode update = null;

            if (CurrentType != TokenType.Semicolon)
                condition = ParseExpressionStatement().expression;
            else
                Advance();
            if (CurrentType != TokenType.RightParenthesis) {
                update = ParseExpression(BindingPower.Default);
                Expect(TokenType.RightParenthesis);
            } else
                Expect(TokenType.RightParenthesis);

            return new ForLoopStatementNode(init, condition, update, ParseStatement());
        } else {
            // for (i in iter)
            Expect(TokenType.In);
            ExpressionNode iterable = ParseExpression(BindingPower.Default);
            Expect(TokenType.RightParenthesis);
            return new ForeachLoopStatementNode(new(name, null), iterable, ParseStatement());
        }
    }

    #endregion

    #region expression parsers

    private ExpressionNode ParseExpression(BindingPower bindingPower) {
        // assume it is a nud
        // continue while there is a led and current binding power < current token binding power
        // left associative
        ExpressionNode left = Nud(CurrentType);

        while (CurrentType.Lookup() > bindingPower)
            left = Led(left, CurrentType, CurrentType.Lookup());

        return left;
    }

    private BinaryExpressionNode ParseBinaryExpression(ExpressionNode left, BindingPower bindingPower) {
        return new(left, Advance(), ParseExpression(bindingPower));
    }

    private PrefixExpressionNode ParsePrefixExpression() {
        return new(Advance(), ParseExpression(BindingPower.Unary));
    }

    private ExpressionNode ParseGroupingExpression() {
        Advance(); // current token is '('. advance past that
        ExpressionNode expression = ParseExpression(BindingPower.Default);
        Expect(TokenType.RightParenthesis); // advance past the ')' token

        return expression;
    }

    // [...]
    private ArrayLiteralExpressionNode ParseArrayExpression() {
        List<ExpressionNode> content = [];

        Expect(TokenType.LeftBracket);

        while (HasTokens && CurrentType != TokenType.RightBracket) {
            // [a = 10] is possible, use logical to avoid it
            content.Add(ParseExpression(BindingPower.Default));

            if (CurrentType != TokenType.RightBracket)
                Expect(TokenType.Coma);
        }

        Expect(TokenType.RightBracket);

        return new([.. content]);
    }

    private PrefixExpressionNode ParsePostfixExpression(ExpressionNode left) {
        BaseToken @operator = CurrentToken;
        // Advance();
        Advance();
        return new(@operator, left);
    }

    private AssignmentExpressionNode ParseAssignmentExpression(ExpressionNode left) {
        return new(left, Advance(), ParseExpression(BindingPower.Assignment));
    }

    private TernaryExpressionNode ParseTernaryExpression(ExpressionNode left) {
        Advance();
        ExpressionNode @true = ParseExpression(BindingPower.Default);
        Expect(TokenType.Colon);
        ExpressionNode @false = ParseExpression(BindingPower.Default);
        return new(left, @true, @false);
    }

    private MemberExpressionNode ParseMemberAccessExpression(ExpressionNode left) {
        if (Advance().Type == TokenType.LeftBracket) // computed
        {
            ExpressionNode rightHandSide = ParseExpression(BindingPower.Member);
            Expect(TokenType.RightBracket);

            return new ComputedMemberAccessExpressionNode(left, rightHandSide);
        }

        return new MemberAccessExpressionNode(left, GetIdentifierName());
    }

    private CallExpressionNode ParseCallExpression(ExpressionNode left) {
        Advance();
        List<ExpressionNode> arguments = [];

        while (HasTokens && CurrentType != TokenType.RightParenthesis) {
            arguments.Add(ParseExpression(BindingPower.Assignment));

            if (HasTokens && CurrentType != TokenType.RightParenthesis)
                Expect(TokenType.Coma);
        }

        Expect(TokenType.RightParenthesis);

        return new(left, [.. arguments]);
    }

    private RangeLiteralExpressionNode ParseRangeLiteral(ExpressionNode left) {
        bool includeEnd = Advance().Type == TokenType.DotDotEqual;
        // prevent 0..1+1 from being valid and enforce 0..(1+1)
        return new(left, ParseExpression(BindingPower.Logical), includeEnd);
    }

    private ClosureExpressionNode ParseClosureExpression() {
        List<FunctionParameter> parameters = [];
        bool paramsArg = false;

        // 0 parameters: ||
        // 1 or more |x...|
        // |x, y|
        // |x: T| optional type annotations, try to infer it
        // || -> T
        if (Advance().Type == TokenType.BitwiseOr) {
            while (HasTokens && CurrentType != TokenType.BitwiseOr) {
                if (CurrentType == TokenType.DotDot) {
                    Advance();
                    paramsArg = true;
                }

                string name = GetIdentifierName();

                parameters.Add(new(name));

                if (paramsArg) {
                    Expect(TokenType.BitwiseOr, "params argument must be the last argument");
                    break;
                }

                if (CurrentType != TokenType.BitwiseOr)
                    Expect(TokenType.Coma);
            }

            if (!paramsArg)
                Expect(TokenType.BitwiseOr);
        }

        return new([.. parameters], ParseStatement(), paramsArg);
    }

    #endregion

    private ExpressionNode Nud(TokenType type) {
        return type switch {
            // literals or identifiers
            TokenType.IntLiteral => new IntExpressionNode((Advance() as IntLiteralToken).value),
            TokenType.FloatLiteral => new DoubleExpressionNode((Advance() as DoubleLiteralToken).value),
            TokenType.StringLiteral => new StringExpressionNode((Advance() as StringLiteralToken).value),
            TokenType.Identifier => new IdentifierExpressionNode((Advance() as IdentifierToken).name),

            // grouping
            TokenType.LeftParenthesis => ParseGroupingExpression(),
            TokenType.LeftBracket => ParseArrayExpression(),

            // prefix
            TokenType.Minus or
            TokenType.Plus or
            TokenType.Increment or
            TokenType.Decrement or
            TokenType.Not or
            TokenType.BitwiseNot => ParsePrefixExpression(),

            // closure
            TokenType.BitwiseOr or
            TokenType.Or => ParseClosureExpression(),

            // let's hope this won't cause issues later down the line
            TokenType.Semicolon => new EmptyExpressionNode(),

            _ => throw new NotImplementedException($"TODO: invalid syntax: unimplemented nud behaviour for \"{type.ToSourceString()}\"")
        };
    }

    private ExpressionNode Led(ExpressionNode left, TokenType type, BindingPower bindingPower) {
        return type switch {
            // literal
            TokenType.DotDot or
            TokenType.DotDotEqual => ParseRangeLiteral(left),

            // logical
            TokenType.And or
            TokenType.Or or

            // relational
            TokenType.LessThan or
            TokenType.LessThanOrEqual or
            TokenType.GreaterThan or
            TokenType.GreaterThanOrEqual or
            TokenType.Equal or

            // additive and multiplicative
            TokenType.Plus or
            TokenType.Minus or
            TokenType.Multiply or
            TokenType.Divide or
            TokenType.Modulo or
            TokenType.Power or

            // bitwise
            TokenType.BitwiseOr or
            TokenType.BitwiseNot or
            TokenType.BitwiseAnd or
            TokenType.BitwiseXor or
            TokenType.ShiftLeft or
            TokenType.ShiftRight => ParseBinaryExpression(left, bindingPower),

            // also relational
            TokenType.QuestionMark => ParseTernaryExpression(left),

            // assignment
            TokenType.Assign or
            TokenType.DotAssign or
            TokenType.PlusAssign or
            TokenType.MinusAssign or
            TokenType.MultiplyAssign or
            TokenType.DivideAssign or
            TokenType.ModuloAssign or
            TokenType.PowerAssign or
            TokenType.ShiftLeftAssign or
            TokenType.ShiftRightAssign or
            TokenType.BitwiseNotAssign or
            TokenType.BitwiseAndAssign or
            TokenType.BitwiseOrAssign or
            TokenType.BitwiseXorAssign => ParseAssignmentExpression(left),

            // member, computed and call
            TokenType.Dot or
            TokenType.LeftBracket => ParseMemberAccessExpression(left),
            TokenType.LeftParenthesis => ParseCallExpression(left),

            // postfix
            TokenType.Increment or
            TokenType.Decrement => ParsePostfixExpression(left),

            _ => throw new NotImplementedException($"unimplemented led behaviour for \"{type.ToSourceString()}\"")
        };
    }
}