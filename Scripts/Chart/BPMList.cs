using System.Collections.Generic;
using PCE.Chart.Util;

namespace PCE.Chart;

public class BPMList {
    private const float defaultBpm = 120;
    public const int invalidIndex = -1;

    public class Entry(double beats, double timeInSeconds, float bpm) {
        // end beats of the previous entry
        // or the start time of this in beats
        // but since beats are accumlated
        // it makes more write to say end beats
        // NOTE: use beats when recalculating values
        public double beats = beats;
        // start time
        public double timeInSeconds = timeInSeconds;
        public float bpm = bpm;
    }

    private readonly List<Entry> elements;
    public Entry Current => elements[cursor];
    private int cursor;

    public BPMList() {
        elements = [new(0, 0, defaultBpm)];
        cursor = 0;
    }

    public BPMList(List<(double, float)> beatBpmPairs) {
        elements = new(beatBpmPairs.Count);
        cursor = 0;

        double time = 0;
        double lastBeats = 0;
        float? lastBpm = null;

        if (beatBpmPairs.Count == 0 || beatBpmPairs[0].Item1 != 0) {
            elements.Add(new(0, 0, defaultBpm)); // 120 is the default bpm
            lastBeats = 0;
            lastBpm = defaultBpm;
        }

        foreach ((double nowBeats, float bpm) in beatBpmPairs) {
            if (lastBpm is float _bpm)
                time += (nowBeats - lastBeats).ToSecond(_bpm);

            lastBeats = nowBeats;
            lastBpm = bpm;
            elements.Add(new(nowBeats, time, bpm));
        }
    }

    public int GetIndexAt(double beats) {
        for (int i = 0; i < elements.Count; i++) {
            Entry entry = elements[i];
            if (entry.beats == beats)
                return i;
        }

        return invalidIndex;
    }

    public int GetClosestIndexAt(double beats) {
        int idx = 0;
        for (; idx < elements.Count; idx++) {
            Entry entry = elements[idx];

            // go until an entry which starts later than beats
            // (or at the same time)
            if (entry.beats < beats)
                continue;

            if (idx != 0) {
                int prevIdx = idx - 1;
                Entry prev = elements[prevIdx];

                // compare the distance between (prev, beats) and (entry, beats)
                // and return the one with the shorter distance
                // since that will be the closer out of the two
                // prev.beats is <= beats
                // enty.beats is >= beats
                return beats - prev.beats > entry.beats - beats ? idx : prevIdx;
            }

            return idx;
        }

        return idx;
    }

    public Entry GetAt(double beats) {
        return elements[GetIndexAt(beats)];
    }

    public void Update(double beats, float bpm) {
        GetAt(beats).bpm = bpm;
        Recalculate();
    }

    private void Add(Entry entry) {
        int idx = 0;
        foreach (Entry element in elements)
            if (element.beats < entry.beats)
                idx++;
            else
                break;

        elements.Insert(idx, entry);
        Recalculate();
    }

    public void Add(double beats, float bpm) {
        Add(new(beats, 0, bpm)); // seconds will be calculated
    }

    public bool HasTime(double beats) {
        return GetIndexAt(beats) != invalidIndex;
    }

    private void Recalculate() {
        double time = 0;
        double lastBeats = 0;
        float? lastBpm = null;

        foreach (Entry entry in elements) {
            if (lastBpm is float _bpm)
                time += (entry.beats - lastBeats).ToSecond(_bpm);

            lastBeats = entry.beats;
            lastBpm = entry.bpm;
            entry.timeInSeconds = time;
        }
    }
}