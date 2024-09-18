namespace PCE.Chartbuild.Runtime;

public enum OpCode : byte {
    Halt, // stop the program
    Push, // push to stack
    Pop, // pop from the stack
    Goto, // set the program counter
    // goto but it won't push the program counter into the goto stack (aka history)
    GotoNoStackPush, // it is just the continue for loops
    // this is for the break statement
    GotoAfterLoop, // when entering the loop the next instruction after the loop is on the goto stack which gets popped (I can already feel the bugs)
    GotoIf, // if, it wont't push to the goto stack
    GotoIfNot, // else, ^same
    // go to the previous position from before the previous goto
    GoBack, // it is just the return in functions
    Assign, // assign a value to a variable
    BinaryOperator, // execute a binary operator (a, b, op is on the stack)
    //user defined functions rely on goto but that cannot call native functions
    // but native functions are also stored as cbfunctions which are stored here
    CallNative, // cbfunction with an index
    // I don't know what this does but auto complete hallucinated it and I took a liking to it
    // oh... it's a java thing
    // TODO, debug breakpoint
    Ldc_I4_0,
}