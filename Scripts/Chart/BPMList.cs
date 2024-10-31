using System.Collections.Generic;
using PCE.Chart.Util;

namespace PCE.Chart;

public class BPMList {
    public struct Entry(double beats, double timeInSeconds, float bpm) {
        public double beats = beats;
        public double timeInSeconds = timeInSeconds;
        public float bpm = bpm;
    }

    private readonly List<Entry> elements;
    private int cursor;

    public BPMList() {
        elements = [];
        cursor = 0;
    }

    public BPMList(List<(double, float)> beatBpmPairs) {
        elements = new(beatBpmPairs.Count);
        cursor = 0;

        double time = 0;
        double lastBeats = 0;
        float? lastBpm = null;

        foreach ((double nowBeats, float bpm) in beatBpmPairs) {
            if (lastBpm is float _bpm)
                time += (nowBeats - lastBeats).ToSecond(_bpm);

            lastBeats = nowBeats;
            lastBpm = bpm;
            elements.Add(new(nowBeats, time, bpm));
        }
    }
}