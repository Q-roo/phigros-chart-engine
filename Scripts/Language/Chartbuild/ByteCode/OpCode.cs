namespace PCE.Chartbuild.Runtime;

public enum OpCode : byte {
    // hlt,
    Halt, // stop the program
    // push, address
    Push, // push to stack
    // pop,
    Pop, // pop from the stack
    // goto, address
    Goto, // set the program counter
    // goto but it won't push the program counter into the goto stack (aka history)
    // gotonsp, address
    GotoNoStackPush, // it is just the continue for loops
    // maybe, a normal goto can be used here as well
    // this is for the break statement
    // //gotoal
    // GotoAfterLoop, // when entering the loop the next instruction after the loop is on the goto stack which gets popped (I can already feel the bugs)
    // gotoi, address
    GotoIf, // if, it wont't push to the goto stack
    // gotofn, address
    GotoIfNot, // else, ^same
    // go to the previous position from before the previous goto
    // gotob,
    GotoBack, // it is just the return in functions
    // relative addresses are 32 bit integers
    GotoRelative, // change the program counter by a number instead of setting it
    GotoRelativeNoStackPush, // goto a relative position without pushing to the goto stack
    GotoRelativeIf, // gotori, address change the program counter by address if the condition from the stack is true
    GotoRelativeIfNot, // go to a relative location if the condition is false (does not push to the goto stack)
    // get's the next value from an iterable
    // or changes the program counter by the offset
    // if it recieved a stop iteration
    IterNextOrGotoRelative, // iternogtr, offset
    // assign, address (asignee), address(value)
    Assign, // assign a value to a variable
    //binop, a, b, op
    BinaryOperator, // execute a binary operator (a, b, op is on the stack)
    //user defined functions rely on goto but that cannot call native functions
    // but native functions are also stored as cbfunctions which are stored here
    // calln, address
    CallNative, // cbfunction with an index
    Call, // call an user defined function
    // I don't know what this does but auto complete hallucinated it and I took a liking to it
    // oh... it's a java thing
    // TODO, debug breakpoint
    Ldc_I4_0,
}