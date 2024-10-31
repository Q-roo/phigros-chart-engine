using System;

namespace PCE.Chart.Util;

public static class TimeConverter {
    public static double SecondToBeat(double second, float bpm) => second / 60d * bpm;
    public static double BeatToSecond(double beat, float bpm) => beat / bpm * 60d;

    public static double ToBeat(this double second, float bpm) => SecondToBeat(second, bpm);
    public static double ToSecond(this double beat, float bpm) => BeatToSecond(beat, bpm);

    public static Triple ToTriple(this double second, Judgeline judgeline) {
        double bar = 0;
        float? lastBpm = null;
        double endTime = 0;

        if (judgeline.bpmChanges.Count == 1)
            bar = second.ToBeat(judgeline.bpmChanges[0]);

        foreach ((double startTimeInSeconds, float bpm) in judgeline.bpmChanges) {
            if (startTimeInSeconds > second) {
                bar += (second - endTime).ToBeat(lastBpm.Value);
                break;
            }
            if (lastBpm is float _bpm) {
                bar += (startTimeInSeconds - endTime).ToBeat(_bpm);
            }
            endTime = startTimeInSeconds;
            lastBpm = bpm;
        }

        int barNumber = (int)Math.Truncate(bar);
        double fraction = bar - barNumber;
        (long mantissa, int exponent) = fraction.GetMantissaAndExponent();

        mantissa = Math.Abs(mantissa);
        exponent = Math.Abs(exponent);
        exponent = (int)Math.Pow(10, exponent);

        ulong gdc = GCD((ulong)mantissa, (ulong)exponent);

        return new(barNumber, (uint)mantissa / (uint)gdc, (uint)exponent / (uint)gdc);
    }

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