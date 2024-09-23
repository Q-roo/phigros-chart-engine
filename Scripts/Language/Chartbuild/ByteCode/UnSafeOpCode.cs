namespace PCE.Chartbuild.Runtime;

public enum UnsafeOpCode : byte {
    HLT, // halt
    DCLV, // declare variable (address)
    ASGN, // assign
    DSPI, // direct stack push int (int)
    DSPD, // direct stack push double (double)
    DSPB, // direct stack push bool (bool)
    DSPN, // direct stack push null
    SPOP, // stack pop
    LCST, // load constant (address)
    ACOL, // array collect (int)
    TRAN, // to range
    TRANI, // to range inclusive
    BINOP, // binary operation (operator)
    PREOP, // prefix operation (operator)
    POSOP, // postfix operation (operator)
    CALL, // call method, arg size, ...args
    CALLN, // call native method, arg size, ...args
    IGET, // identifier get (string constant address)
    MGET, // member get
    APUSH, // push an address onto the jump stack
    APOP, // pop an address from the jump stack
    JMP, // jump (address)
    JMPI, // jump if (address)
    JMPN, // jump if not (address)
    JMPE, // jump to end (next LEND, replace this with a normal jump)
    ITER, // to iterable (pushes the success to the stack)
    ITERN, // iter next
    LEND, // loop end (replace this with an address once the loop's size is known)
    CPTR, // capture (all variables and the unused ones will be removed)
    CSTART, // closure start
    CEND, // closure end, construct the closure object
}

public static class UnsafeOpCodeExtensions {
    public static byte AsByte(this UnsafeOpCode opcode) => (byte)opcode;
}