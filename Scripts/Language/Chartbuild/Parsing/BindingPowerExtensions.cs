namespace PCE.Chartbuild;

public static class BindingPowerExtensions
{
    public static BindingPower Lookup(this TokenType type)
    {
        return type switch
        {
            // TokenType.Literal or
            // TokenType.IntLiteral or
            // TokenType.FloatLiteral or
            // TokenType.StringLiteral or
            // TokenType.Identifier => BindingPower.Primary,

            TokenType.And or
            TokenType.Or or
            // .. and ..= also have the lowest presedence
            TokenType.DotDot or
            TokenType.DotDotEqual => BindingPower.Logical,

            TokenType.QuestionMark => BindingPower.Ternary,

            TokenType.BitwiseAnd or
            TokenType.BitwiseOr or
            TokenType.BitwiseXor or
            TokenType.BitwiseNot => BindingPower.Bitwise,

            TokenType.ShiftLeft or
            TokenType.ShiftRight => BindingPower.BitShift,

            TokenType.LessThan or
            TokenType.GreaterThan or
            TokenType.LessThanOrEqual or
            TokenType.GreaterThanOrEqual or
            TokenType.Equal or
            TokenType.QuestionMark => BindingPower.Relational,

            TokenType.Plus or
            TokenType.Minus => BindingPower.Additive,

            TokenType.Multiply or
            TokenType.Divide or
            TokenType.Modulo or
            TokenType.Power => BindingPower.Multiplicative,

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
            TokenType.BitswiseNotAssign or
            TokenType.BitwiseAndAssign or
            TokenType.BitwiseOrAssign or
            TokenType.BitwiseXorAssign => BindingPower.Assignment,

            TokenType.Increment or
            TokenType.Decrement => BindingPower.Unary,

            TokenType.Dot or
            TokenType.LeftBracket => BindingPower.Member,

            TokenType.LeftParenthesis => BindingPower.Call,

            // TokenType.Coma => BindingPower.Coma,

            _ => BindingPower.Default,
        };
    }

    public static BindingPower TypeLookup(this TokenType type)
    {
        return type switch
        {
            // TokenType.Coma => BindingPower.Coma,
            TokenType.LessThan => BindingPower.Call,
            _ => BindingPower.Default
        };
    }
}