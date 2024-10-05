namespace PCE.Chartbuild;

public enum OperatorType : byte {
    // comparison
    LessThan, // <
    LessThanOrEqual, // <=
    GreaterThan, // >
    GreaterThanOrEqual, // >=
    Equal, // ==
    NotEqual, // !=

    // logical
    And, // &&
    Or, // ||
    Not, // !

    // bitwise
    BitwiseAnd, // &
    BitwiseOr, // |
    BitwiseNot, // ~
    BitwiseXor, // ^
    ShiftLeft, // <<
    ShiftRight, // >>

    // math
    Plus, // +
    Minus, // -
    Multiply, // *
    Power, // **
    Divide, // /
    Modulo, // %

    Increment, // ++
    Decrement, // --
};