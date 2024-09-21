namespace PCE.Chartbuild;

// TODO
// * NullCoalescing, // ??
// * NullCoalescingAssign, // ??=

public enum TokenType : byte
{
    Unknown = 0,

    // basic
    Identifier,
    Literal,
    StringLiteral,
    IntLiteral,
    FloatLiteral,

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

    // assignment
    Assign, // =
    DotAssign, // .=
    PlusAssign, // +=
    MinusAssign, // -=
    MultiplyAssign, // *=
    PowerAssign, // **=
    DivideAssign, // /=
    ModuloAssign, // %=
    ShiftLeftAssign, // <<=
    ShiftRightAssign, // >>=
    BitwiseAndAssign, // &=
    BitwiseOrAssign, // |=
    BitwiseXorAssign, // ^=
    BitswiseNotAssign, // ~=
    Increment, // ++
    Decrement, // --

    // controlflow
    If, // if
    Else, // else
    QuestionMark, // ?
    For, // for
    While, // while
    Break, // break
    Continue, // continue
    Return, // return
    Switch, // switch
    Case, // case

    // keyword
    Let, // let
    Const, // const
    Fn, // fn
    In, // in

    // punctuation
    LeftBracket, // [
    RightBracket, // ]
    LeftBrace, // {
    RightBrace, // }
    LeftParenthesis, // (
    RightParenthesis, // )
    Coma, // ,
    Semicolon, // ;
    Dot, // .
    DotDot, // ..
    DotDotEqual, // ..= (inclusive range)
    Colon, // :
    RightArrow, // ->
    // RightArrowThick, // =>
    Hash, // #

    Eof
}