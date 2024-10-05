namespace PCE.Chartbuild;

public static class OperatorTypeExtensions {
    public static string ToSourceString(this OperatorType type) => type switch {
            OperatorType.LessThan           => "<",
            OperatorType.LessThanOrEqual    => "<=",
            OperatorType.GreaterThan        => ">",
            OperatorType.GreaterThanOrEqual => ">=",
            OperatorType.Equal              => "==",
            OperatorType.NotEqual           => "!=",
            OperatorType.And                => "&&",
            OperatorType.Or                 => "||",
            OperatorType.Not                => "!",
            OperatorType.BitwiseAnd         => "&",
            OperatorType.BitwiseOr          => "|",
            OperatorType.BitwiseNot         => "~",
            OperatorType.BitwiseXor         => "^",
            OperatorType.ShiftLeft          => "<<",
            OperatorType.ShiftRight         => ">>",
            OperatorType.Plus               => "+",
            OperatorType.Minus              => "-",
            OperatorType.Multiply           => "*",
            OperatorType.Power              => "**",
            OperatorType.Divide             => "/",
            OperatorType.Modulo             => "%",
            OperatorType.Increment          => "++",
            OperatorType.Decrement          => "--",
            _                               => "??"
    };
}