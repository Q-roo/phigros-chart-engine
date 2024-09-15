using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace PCE.Chartbuild;

public static class Lexer
{
    public const Version ForVersion = Version.V_0;
    public static BaseToken[] Parse(string source)
    {
        if (string.IsNullOrEmpty(source))
            return [];

        // source += '\n'; // make sure that source always has an extra line
        List<BaseToken> tokens = [];
        int line = 1;
        int column = 1;

        for (int i = 0; i < source.Length; i++, column++)
        {
            char c = source[i];

            if (char.IsWhiteSpace(c))
            {
                if (c == '\n')
                {
                    line++;
                    column = 0; // the next cycle will increment it to 1
                }

                continue;
            }

            if (c == '"' || c == '\'')
            {
                char delimiter = c;
                int start = i + 1;
                int startLine = line;
                int startColumn = column;

                do
                {
                    i++;  // thanks to this, I can't use switch or else if
                    column++;

                    if (i == source.Length)
                        throw new UnexpectedTokenException(new StringLiteralToken(line, column, "!!error"), $"EOF found, '{delimiter}' expected");

                    c = source[i];

                    if (c == '\n')
                    {
                        line++;
                        column = 1;
                    }
                }
                while (c != delimiter && source[i - 1] != '\\');

                tokens.Add(new StringLiteralToken(startLine, startColumn, source[start..i]));
                continue; // current character is the delimiter
            }

            if (char.IsAsciiDigit(c))
            {
                // possible
                // - binary (0b...)
                // - hexadecimal (0x...)
                // - decimal (1...)
                // - float (0.2...)
                // - scientific (0.1e+5)

                int start;

                if (c == '0' && i < source.Length - 1)
                {
                    switch (source[i + 1])
                    {
                        case 'b':
                            ++i; // move to the b character
                            start = i + 1;

                            do
                            {
                                i++;
                                if (i == source.Length)
                                    break;

                                c = source[i];
                            }
                            while (c == '0' || c == '1');

                            // the parser will handle invalid binary characters (e.g.: 0b12)
                            // there's no BinaryLiteralToken yet so include 0b
                            column += i - start - 1;
                            GD.Print(source[start..i]);
                            tokens.Add(new IntLiteralToken(line, column, int.Parse(source[start..i], NumberStyles.BinaryNumber)));
                            i--;
                            column += 2; // the 0b part
                            continue;
                        case 'x':
                            ++i;
                            start = i + 1;

                            do
                            {
                                i++;
                                if (i == source.Length)
                                    break;

                                c = source[i];
                            }
                            while (char.IsAsciiHexDigit(c));

                            column += i - start + 1;
                            tokens.Add(new IntLiteralToken(line, column, int.Parse(source[start..i], NumberStyles.HexNumber)));
                            i--;
                            column += 2;
                            continue;
                    }
                }

                start = i;
                bool isInt = true;

                do
                {
                    i++;
                    if (i == source.Length)
                        break;

                    c = source[i];
                    if (c == '.')
                    {
                        isInt = false;
                        continue;
                    }
                }
                while (char.IsAsciiDigit(c));

                column += i - start - 1;

                if (isInt)
                    tokens.Add(new IntLiteralToken(line, column, int.Parse(source[start..i])));
                else
                    tokens.Add(new FloatLiteralToken(line, column, double.Parse(source[start..i])));

                i--; // i is at the next character
                continue;

            }

            if (c == '/')
                // possible:
                // - // ab
                // - /* ab */
                // - a /= b
                // - a / b

                if (i < source.Length - 1)
                    switch (source[i + 1])
                    {
                        case '/':
                            do
                            {
                                ++i;
                                if (i == source.Length)
                                    return [.. tokens, new Token(line, column, TokenType.Eof)];

                                c = source[i];
                            }
                            while (c != '\n'); // should take care of CRLF as well

                            line++;
                            column = 0;
                            continue;
                        case '*':
                            i += 2; // prevent /*/ from being valid
                            column += 2;

                            // edge case:
                            // /*
                            // */
                            if (source[i] == '\n')
                            {
                                line++;
                                column = 0;
                            }

                            do
                            {
                                ++i;
                                column++;

                                if (i == source.Length)
                                    throw new UnexpectedTokenException(new StringLiteralToken(line, column, "!!error"),"EOF found, '*/' expected");

                                c = source[i];

                                if (c == '\n')
                                {
                                    line++;
                                    column = 0;
                                }
                            }
                            while (c != '/' && source[i - 1] != '*');
                            continue;
                        case '=':
                            i++;
                            column++;
                            tokens.Add(new Token(line, column, TokenType.DivideAssign));
                            continue;
                        default:
                            tokens.Add(new Token(line, column, TokenType.Divide));
                            continue;
                    }
                else
                {
                    tokens.Add(new Token(line, column, TokenType.Divide));
                    continue;
                }


            // naming things is hard
            bool brokenNested = false;
            foreach ((char _c, TokenType _type) in new (char, TokenType)[]
            {
                ('#', TokenType.Hash),
                ('[', TokenType.LeftBracket),
                (']', TokenType.RightBracket),
                ('{', TokenType.LeftBrace),
                ('}', TokenType.RightBrace),
                ('(', TokenType.LeftParenthesis),
                (')', TokenType.RightParenthesis),
                (',', TokenType.Coma),
                (';', TokenType.Semicolon),
                (':', TokenType.Colon),
                ('?', TokenType.QuestionMark)
            })
            {
                // will increment i by one if the character matches
                if (i == source.Length)
                    break;

                if (TryChar(in source, ref i, _c))
                {
                    tokens.Add(new Token(line, column, _type));
                    brokenNested = true;
                }
            }

            if (brokenNested) // at the next character which should go trough the loop
            {
                i--;
                continue;
            }

            TokenType type;
            int columnChange;
            // / and /= are already handled by the comment tester
            if (
                TryCharVariantsAhead(in source, ref i, '+', TokenType.Plus, out type, out columnChange, ("+", TokenType.Increment), ("=", TokenType.PlusAssign)) ||
                TryCharVariantsAhead(in source, ref i, '-', TokenType.Minus, out type, out columnChange, ("-", TokenType.Decrement), ("=", TokenType.MinusAssign), (">", TokenType.RightArrow)) ||
                TryCharVariantsAhead(in source, ref i, '^', TokenType.BitwiseXor, out type, out columnChange, ("=", TokenType.BitwiseXorAssign)) ||
                TryCharVariantsAhead(in source, ref i, '~', TokenType.BitwiseNot, out type, out columnChange, ("=", TokenType.BitswiseNotAssign)) ||
                TryCharVariantsAhead(in source, ref i, '|', TokenType.BitwiseOr, out type, out columnChange, ("|", TokenType.Or), ("=", TokenType.BitwiseOrAssign)) ||
                TryCharVariantsAhead(in source, ref i, '&', TokenType.BitwiseAnd, out type, out columnChange, ("&", TokenType.And), ("=", TokenType.BitwiseAndAssign)) ||
                TryCharVariantsAhead(in source, ref i, '<', TokenType.LessThan, out type, out columnChange, ("<", TokenType.ShiftLeft), ("=", TokenType.LessThanOrEqual), ("<=", TokenType.ShiftLeftAssign)) ||
                TryCharVariantsAhead(in source, ref i, '>', TokenType.GreaterThan, out type, out columnChange, (">", TokenType.ShiftRight), ("=", TokenType.GreaterThanOrEqual), ("<=", TokenType.ShiftRightAssign)) ||
                TryCharVariantsAhead(in source, ref i, '%', TokenType.Modulo, out type, out columnChange, ("=", TokenType.ModuloAssign)) ||
                TryCharVariantsAhead(in source, ref i, '*', TokenType.Multiply, out type, out columnChange, ("*", TokenType.Power), ("=", TokenType.MultiplyAssign), ("*=", TokenType.PowerAssign)) ||
                TryCharVariantsAhead(in source, ref i, '=', TokenType.Assign, out type, out columnChange, ("=", TokenType.Equal) /*, (">", TokenType.RightArrowThick) */) ||
                TryCharVariantsAhead(in source, ref i, '.', TokenType.Dot, out type, out columnChange, (".", TokenType.DotDot), ("=", TokenType.DotAssign), (".=", TokenType.DotDotEqual)) ||
                TryCharVariantsAhead(in source, ref i, '!', TokenType.Not, out type, out columnChange , ("=", TokenType.NotEqual))
            )
            {
                tokens.Add(new Token(line, column, type));
                column += columnChange;
                continue;
            }

            // reuse brokenNested
            foreach ((string _keyword, TokenType _type) in new (string, TokenType)[]
            {
                ("let", TokenType.Let),
                ("const", TokenType.Const),
                ("fn", TokenType.Fn),
                ("in", TokenType.In),
                ("if", TokenType.If),
                ("else", TokenType.Else),
                ("for", TokenType.For),
                ("while", TokenType.While),
                ("break", TokenType.Break),
                ("continue", TokenType.Continue),
                ("return", TokenType.Return),
                ("switch", TokenType.Switch),
                ("case", TokenType.Case)
            })
            {
                if (TryKeyword(in source, ref i, _keyword))
                {
                    tokens.Add(new Token(line, column, _type));
                    column += _keyword.Length - 1;
                    brokenNested = true;
                    break;
                }
            }

            if (brokenNested)
                continue;

            // char is the start of an identifier
            {
                int start = i;
                do
                {
                    i++;
                    if (i == source.Length)
                        break;

                    c = source[i];
                }
                while (char.IsLetterOrDigit(c) || c == '_'); // cases like 1name are taken care of by the parsers above

                column += i - start - 1;
                tokens.Add(new IdentifierToken(line, column, source[start..i]));
                // could be a generic:
                // G<T>
                // G<T1, T2>
                // G<T1, G1<G2<T2, T3>>>
                // G<T1, G1<T2, T3>, T4>
                // not a generic
                // a<g
                // allowed characters < > \s [[:alnum:]] _ ,
                if (c == '<' && i < source.Length - 1)
                {
                    int gi = i + 1;
                    char gc = source[gi];
                    if (char.IsLetter(gc))
                    {
                        // every time the lexer encounters a < character, increment it by one
                        // every time the lexer encounters a > character, decrement it by one
                        // it is a generic until this reaches 0 or eof
                        int closingNeeded = 1;
                        bool isGeneric = true;

                        for (; closingNeeded > 0 && gi < source.Length; gi++)
                        {
                            gc = source[gi];

                            if (gc == '<')
                            {
                                closingNeeded++;
                                continue;
                            }
                            else if (gc == '>')
                            {
                                closingNeeded--;
                                continue;
                            }
                            else if (!(char.IsWhiteSpace(gc) || char.IsLetterOrDigit(gc) || gc == '_' || gc == ','))
                            {
                                isGeneric = false;
                                break;
                            }
                        }

                        // if closing needed reached 0, it is a generic
                        // else, assume that it is not one

                        if (isGeneric)
                        {
                            string genericIdentifier = source[i..gi];

                            // should be at the last > character
                            i += genericIdentifier.Length - 1;
                            // column += genericIdentifier.Length - 1;

                            for (int j = 0; j < genericIdentifier.Length; j++, column++)
                            {
                                gc = genericIdentifier[j];

                                // FIXME: ~~I regret not using regex~~, the column number doesn't seem to be broken anywhere else
                                if (char.IsWhiteSpace(gc))
                                    continue;
                                else if (gc == '<')
                                    tokens.Add(new Token(line, column + 1, TokenType.LessThan));
                                else if (gc == '>')
                                    tokens.Add(new Token(line, column + 1, TokenType.GreaterThan));
                                else if (gc == ',')
                                    tokens.Add(new Token(line, column + 1, TokenType.Coma));
                                else if (char.IsDigit(gc))
                                    throw new UnexpectedTokenException(new IdentifierToken(line, column, "!!error!!"),"identifier names cannot start with numbers");
                                else
                                {
                                    start = j;
                                    do
                                    {
                                        j++;

                                        if (j == genericIdentifier.Length)
                                            break;

                                        gc = genericIdentifier[j];
                                    }
                                    while (char.IsLetterOrDigit(gc) || gc == '_');

                                    column += j - start - 1;
                                    tokens.Add(new IdentifierToken(line, column, genericIdentifier[start..j]));
                                    j--;
                                    continue;
                                }
                            }

                            continue;
                        }
                        else
                        {
                            i--;
                            continue;
                        }
                    }
                }
                else
                    i--;
                // column--;
                continue;
            }
        }

        return [.. tokens, new Token(line, column, TokenType.Eof)];
    }

    private static bool TryChar(in string source, ref int position, char c)
    {
        if (source[position] == c)
        {
            position++;
            return true;
        }
        return false;
    }

    private static bool TryCharVariantsAhead(in string source, ref int position, char c, TokenType cType, out TokenType match, out int columnChange, params (string, TokenType)[] variants)
    {
        match = TokenType.Unknown;
        columnChange = 0;

        if (source[position] == c)
        {
            Array.Sort(variants, (a, b) => b.Item1.Length.CompareTo(a.Item1.Length));

            foreach ((string variant, TokenType type) in variants)
            {
                if (position + variant.Length < source.Length)
                {
                    string full = c + variant;
                    if (source.Substring(position, variant.Length + 1) == full)
                    {
                        position += variant.Length;
                        columnChange = variant.Length;
                        match = type;
                        return true;
                    }
                }
            }

            match = cType;

            // position++; // the loop will increment it
            return true;
        }

        return false;
    }

    private static bool TryKeyword(in string source, ref int position, string keyword)
    {
        if (position + keyword.Length - 1 >= source.Length)
            return false;

        // cases like aif, etc should be handled by default
        if (source.Substring(position, keyword.Length) == keyword)
        {
            // test for cases like ifa
            if (position + keyword.Length < source.Length - 1 && char.IsLetter(source[position + keyword.Length]))
                return false;

            position += keyword.Length - 1; // go to last character of the keyword
            return true;
        }

        return false;
    }
}