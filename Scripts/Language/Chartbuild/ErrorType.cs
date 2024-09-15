namespace PCE.Chartbuild;

public enum ErrorType
{
    NoError, // some functions return a code, this is for when evrything went alright
    UnexpectedToken, // "string' <- should be "
    MissingToken, // "string
    CannotStartStatement, // else ... without an if
    NotSupported, // switch
    InvalidArgument, // 1.add("str")
    InvalidType, // foo: str = 1
    MissingMember, // foo.bar <- there is no member named bar
    DividedByZero, // n / 0
    SetConstant, // const a = 0; a = 1;
    UninitalizedValue, // const a; print(a)
    SetNull, // SetValue(null); there's a null constant, use that
    DuplicateIdentifier, // var a; var a;
    NotCompileTimeConstant, // error for the analyzer. expressions like this (const a = 1 + 2) get removed and stored in the scope directly. with this error, do not remove it
    OutOfRange, // [-1] orr [length]
    DoesNotStartWithVersion, // first statement should be the version command
    InvalidVersion, // #version nonexsitent_version
    InvalidCommand, // #nonexistent_command
    NotCallable, // "a"()
    NullValue, // null + 1

    // errors for the statement evaluator
    BreakLoop, // not really an error
    ContinueLoop, // also not a real error
}