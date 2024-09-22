namespace PCE.Chartbuild.Runtime;

public enum UnsafeOpCode : byte {
    HLT, // halt
    DCLV, // declare variable (address)
    ASGN, // assign
    FRO, // flag read only (address)
    DSPI, // direct stack push int (int)
    DSPD, // direct stack push double (double)
    DSPB, // direct stack push bool (bool)
    DSPN, // direct stack push null
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
    ITER, // to iterable (pushes the success to the stack)
}

public static class UnsafeOpCodeExtensions {
    public static byte AsByte(this UnsafeOpCode opcode) => (byte)opcode;
}