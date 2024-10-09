namespace PCE.Chart.Util;

public static class TimeConverter {
    public static double SecondToBeat(double second, float bpm) => second / 60d * bpm;
    public static double BeatToSecond(double beat, float bpm) => beat / bpm * 60d;

    public static double ToBeat(this double second, float bpm) => SecondToBeat(second, bpm);
    public static double ToSecond(this double beat, float bpm) => BeatToSecond(beat, bpm);
}