using System.Collections;
using System.Collections.Generic;
using Godot;
using PCE.Chart.Util;

namespace PCE.Chart;

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

public class BPMList : IEnumerable<Entry> {
    private const float defaultBpm = 120;
    public const int invalidIndex = -1;

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

    public double BeatToSecond(double beat) {
        for (; cursor < elements.Count - 1; cursor++)
            if (Current.beats > beat)
                break;

        while (cursor >= 0 && Current.beats > beat)
            cursor--;

        return Current.timeInSeconds + (beat - Current.beats).ToSecond(Current.bpm);
    }

    public double TripleToSecond(Triple triple) {
        return BeatToSecond(triple.ToBeat());
    }

    public double SecondToBeat(double second) {
        for (; cursor < elements.Count - 1; cursor++)
            if (Current.timeInSeconds > second)
                break;

        while (cursor >= 0 && Current.timeInSeconds > second)
            cursor--;

        return Current.beats + (second - Current.timeInSeconds).ToBeat(Current.bpm);
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

    public IEnumerable<float> GetBpms() {
        foreach (Entry entry in elements)
            yield return entry.bpm;
    }

    public IEnumerable<double> GetStartTimesInSeconds() {
        foreach (Entry entry in elements)
            yield return entry.timeInSeconds;
    }

    public IEnumerable<double> GetStartTimesInBeats() {
        foreach (Entry entry in elements)
            yield return entry.beats;
    }

    public IEnumerator<Entry> GetEnumerator() {
        return elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public Entry GetAt(double beats) {
        return elements[GetIndexAt(beats)];
    }

    public Entry GetClosestAt(double beats) {
        return elements[GetClosestIndexAt(beats)];
    }

    public Entry Last() {
        return elements[^1];
    }

    private void UpdateBpm(Entry entry, float bpm) {
        entry.bpm = bpm;
        Recalculate();
    }

    public void UpdateBpm(double beats, float bpm) {
        UpdateBpm(GetAt(beats), bpm);
    }

    public void UpsertBpm(double beats, float bpm) {
        int idx = GetIndexAt(beats);
        if (idx == invalidIndex)
            Add(beats, bpm);
        else
            UpdateBpm(elements[idx], bpm);
    }

    public void UpdateStartBeats(double fromBeats, double toBeats) {
        int idx = GetIndexAt(fromBeats);
        if (idx != invalidIndex) {
            Entry entry = elements[idx];
            elements.RemoveAt(idx);
            entry.beats = toBeats;
            Add(entry);
        }
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

    public bool Remove(double beats) {
        int idx = GetIndexAt(beats);
        if (idx != invalidIndex) {
            elements.RemoveAt(idx);
            Recalculate();
            return true;
        }

        return false;
    }

    public void Clear() {
        elements.Clear();
        elements.Add(new(0, 0, defaultBpm));
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