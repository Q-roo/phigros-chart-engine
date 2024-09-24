namespace PCE.Chartbuild.Runtime;

using Address = ushort;

public enum UnsafeOpCode : byte {
    HLT, // halt
    DCLV, // declare variable (address)
    ASGN, // assign
    DSPA, // direct stack push address (address)
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
    // these were for function calls and returns but this should be handled by the vm instead
    // APUSH, // push an address onto the jump stack
    // APOP, // pop an address from the jump stack
    // the values for the jump instructions are on the stack
    JMP, // jump
    JMPI, // jump if
    JMPN, // jump if not
    JMPS, // jump to start (current LSTART, replace this with a normal jump)
    JMPE, // jump to end (next LEND, replace this with a normal jump)
    JMPNE, // jump if not to end (next LEND, replace this with a normal jumpn)
    RET, // return
    ITER, // to iterable (pushes the success to the stack)
    ITERN, // iter next
    // for the first phase, the addresses are relative to their chunk
    // these will be removed once everything is in one chunk
    LSTART, // loop start
    LEND, // loop end
    CPTR, // capture (all variables and the unused ones will be removed)
    CSTART, // closure start
    CEND, // closure end, construct the closure object
}

public static class UnsafeOpCodeExtensions {
    public static byte AsByte(this UnsafeOpCode opcode) => (byte)opcode;

    public static byte SizeOf(this UnsafeOpCode opcode) => opcode switch {
        UnsafeOpCode.DCLV => sizeof(byte) + sizeof(Address),
        UnsafeOpCode.DSPA => sizeof(byte) + sizeof(Address),
        UnsafeOpCode.DSPI => sizeof(byte) + sizeof(int),
        UnsafeOpCode.DSPD => sizeof(byte) + sizeof(double),
        UnsafeOpCode.DSPB => sizeof(byte) + sizeof(bool),
        UnsafeOpCode.LCST => sizeof(byte) + sizeof(Address),
        UnsafeOpCode.ACOL => sizeof(byte) + sizeof(int),
        // token type is also a byte
        UnsafeOpCode.BINOP => sizeof(byte) + sizeof(byte),
        UnsafeOpCode.PREOP => sizeof(byte) + sizeof(byte),
        UnsafeOpCode.POSOP => sizeof(byte) + sizeof(byte),
        UnsafeOpCode.CALL => sizeof(byte) + sizeof(int),
        UnsafeOpCode.CALLN => sizeof(byte) + sizeof(int),
        UnsafeOpCode.IGET => sizeof(byte) + sizeof(Address),

        _ => sizeof(byte)
    };
}