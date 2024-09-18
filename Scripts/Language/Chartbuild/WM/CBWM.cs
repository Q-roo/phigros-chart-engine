using System;

namespace PCE.Chartbuild.Runtime;

public class WM {
    const uint maxVariables = 255;

    private readonly byte[] byteCode;
    private readonly CBVariable[] variables = new CBVariable[maxVariables];

    private uint programCounter = 0;

    public void Evaluate() {
        if (byteCode.Length == 0)
            return;

            byte a = 255;

        for (; ; ) {
            switch (Read()) {
                case (byte)OpCode.Halt:
                    return;
                default:
                    throw new InvalidOperationException($"unknown instruction ({byteCode[programCounter]})");
            }
        }
    }

    private byte Read() {
        return byteCode[programCounter++];
    }
}