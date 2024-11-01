using System;

namespace PCE.Chart.Util;

public static class TimeConverter {
    public static double SecondToBeat(double second, float bpm) => second / 60d * bpm;
    public static double BeatToSecond(double beat, float bpm) => beat / bpm * 60d;

    public static double ToBeat(this double second, float bpm) => SecondToBeat(second, bpm);
    public static double ToSecond(this double beat, float bpm) => BeatToSecond(beat, bpm);

    public static double ToBeat(this double second, BPMList bpmList) => bpmList.SecondToBeat(second);
    public static double ToSecond(this double beat, BPMList bpmList) => bpmList.BeatToSecond(beat);

    public static double ToBeat(this double second, Chart chart) => ToBeat(second, chart.bpmList);
    public static double ToSecond(this double beat, Chart chart) => ToSecond(beat, chart.bpmList);

    public static Triple ToTriple(this double beat) {
        int bar = (int)Math.Truncate(beat);
        double fraction = beat - bar;
        (long mantissa, int exponent) = fraction.GetMantissaAndExponent();

        mantissa = Math.Abs(mantissa);
        exponent = Math.Abs(exponent);
        exponent = (int)Math.Pow(10, exponent);

        ulong gdc = GCD((ulong)mantissa, (ulong)exponent);

        return new(bar, (uint)mantissa / (uint)gdc, (uint)exponent / (uint)gdc);
    }

    public static Triple ToTriple(this double second, BPMList bpmList) => ToTriple(second.ToBeat(bpmList));
    public static Triple ToTriple(this double second, Chart chart) => ToTriple(second, chart.bpmList);

    // https://stackoverflow.com/a/41766138
    public static ulong GCD(ulong a, ulong b) {
        while (a != 0 && b != 0) {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a | b;
    }

    public static (long mantissa, int exponent) GetMantissaAndExponent(this float f) => GetMantissaAndExponent((decimal)f);
    public static (long mantissa, int exponent) GetMantissaAndExponent(this double d) => GetMantissaAndExponent((decimal)d);

    // https://www.reddit.com/r/csharp/comments/g508fy/calculating_the_mantissa_and_exponent_of_a/
    public static (long mantissa, int exponent) GetMantissaAndExponent(this decimal d) {
        long mantissa = 0;
        int exponent = 0;

        if (d == 0.0m)
            return (mantissa, exponent);

        d = Math.Round(d, 6);
        uint[] bits = (uint[])(object)decimal.GetBits(d);
        mantissa = (long)((bits[2] * 4294967296m * 4294967296m) + (bits[1] * 4294967296m) + bits[0]);

        if (d < 0)
            mantissa = -mantissa;

        exponent = -((int)(uint)(bits[3] >> 16) & 31);
        return (mantissa, exponent);
    }
}