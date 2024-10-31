namespace PCE.Chart;

// phira calls it triples and I am too lazy to be original

public struct Triple {
    public int BarNumber {get; set;}
    public uint Numerator {get; set;}
    public uint Denominator {get; set;}

    public Triple() {
        BarNumber = 0;
        Numerator = 0;
        Denominator = 1;
    }

    public Triple(int barNumber, uint numerator, uint denominator) {
        BarNumber = barNumber;
        Numerator = numerator;
        Denominator = denominator;
    }

    public override readonly string ToString() {
        return $"{BarNumber}:{Numerator}/{Denominator}";
    }
}