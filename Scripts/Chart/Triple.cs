namespace PCE.Chart;

// phira calls it triples and I am too lazy to be original

public struct Triple(int barNumber, uint numerator, uint denominator) {
    public static Triple Default => new(0, 0, 1);
    public int BarNumber { get; set; } = barNumber;
    public uint Numerator { get; set; } = numerator;
    public uint Denominator { get; set; } = denominator;

    public override readonly string ToString() {
        return $"{BarNumber}:{Numerator}/{Denominator}";
    }
}