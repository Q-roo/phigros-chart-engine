using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using PCE.Chart.Util;
using PCE.Chartbuild.Bindings;
using PCE.Editor;

namespace PCE.Chart;

public partial class Chart : Node2D, ICBExposeable {
    public static CompatibilityLevel Platform => CompatibilityLevel.PCE;
    public readonly TransformGroup rootGroup = new("root");
    // time is in seconds
    public double CurrentTime { get; private set; }
    public double DeltaTime { get; private set; }
    public bool JustStarted { get; private set; }
    public bool IsInitalized { get; private set; }
    // TODO: current score

    private ulong timeBegin;
    private double audioDelay;

    private readonly List<Event> inactiveEvents = [];
    private readonly List<Event> activeEvents = [];
    public readonly HashSet<StringName> signals  = [];
    public readonly List<Judgeline> judgelines = [];

    private readonly AudioStreamPlayer audioPlayer = new();

    public double MusicLengthInSeconds => audioPlayer.Stream.GetLength();

    private double _previousMusicPlaybackPositionInSeconds;
    public double MusicPlaybackPositionInSeconds {
        get {
            double time = audioPlayer.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix() - AudioServer.GetOutputLatency();
            if (time < _previousMusicPlaybackPositionInSeconds)
                return _previousMusicPlaybackPositionInSeconds;

            _previousMusicPlaybackPositionInSeconds = time;
            return time;
        }
    }
    public float MusicVolume {
        get => audioPlayer.VolumeDb;
        set => audioPlayer.VolumeDb = value;
    }

    public Chart() {
        AddChild(audioPlayer);
        AddChild(rootGroup);
        Reset();
        audioPlayer.Stream = Project.SelectedProject.Audio;
    }

    public void StartMusic() {
        audioPlayer.Play();
        SoftReset();
        BeginRender();
    }

    public void StopMusic() {
        audioPlayer.Stop();
    }

    public void PauseMusic() {
        audioPlayer.StreamPaused = true;
    }

    public void ResumeMusic() {
        audioPlayer.StreamPaused = false;
    }

    public void TogglePaused() {
        audioPlayer.StreamPaused = !audioPlayer.StreamPaused;
    }

    public void TogglePlaying() {
        audioPlayer.Playing = !audioPlayer.Playing;
        if (audioPlayer.Playing) {
            SoftReset();
            BeginRender();
        }
    }

    public void SeekTo(double timeInSeconds) {
        audioPlayer.Seek((float)timeInSeconds);
        _previousMusicPlaybackPositionInSeconds = timeInSeconds;
    }

    public void SoftReset() {
        SetProcess(false);
        JustStarted = false;
        IsInitalized = false;
        CurrentTime = 0;
        DeltaTime = 0;
        _previousMusicPlaybackPositionInSeconds = 0;
    }

    public void Reset() {
        SoftReset();
        foreach (Node child in rootGroup.GetChildren()) {
            child.Free(); // the script's execution will start in this frame
        }

        inactiveEvents.Clear();
        activeEvents.Clear();
        signals.Clear();
        judgelines.Clear();
    }

    public void BeginRender() {
        timeBegin = Time.GetTicksUsec();
        audioDelay = AudioServer.GetTimeToNextMix() + AudioServer.GetOutputLatency();
        SetProcess(true);
        // add on start events
        JustStarted = true;
        AddActiveEvents();
        JustStarted = false;
        IsInitalized = true;
        SetNoteYPositions();
        audioPlayer.Play();
    }

    private void SetNoteYPositions() {
        foreach (Judgeline judgeline in judgelines)
            foreach (Note note in judgeline.notes) {
                Vector2 position = note.Position;

                double y = CalculateYPosition(note.time, judgeline);

                position.Y -= (float)y;

                if (note.type == NoteType.Hold) {
                    double heigth = CalculateYPosition(note.time + note.holdTime, judgeline) - y;
                    Vector2 scale = note.Scale;
                    // scale.Y = (float)(heigth / note.Texture.GetSize().Y);
                    scale.Y = (float)(heigth / note.Texture.GetSize().Y);
                    note.Scale = scale;
                }

                // Y offset
                // (y center is at the center of the sprite but it needs to be on the bottom)
                // position.Y -= note.Texture.GetSize().Y * note.Scale.Y / 2;
                position.Y -= note.Texture.GetSize().Y * note.Scale.Y / 2;

                note.Position = position;
            }
    }

    public double CalculateYPosition(double time, Judgeline judgeline) {
        double y = 0;

        // times of bpm changes in seconds
        double[] keys = [..judgeline.bpmChanges.Keys];

        Debug.Assert(keys.Length != 0 || keys[0] != 0, "a judgeline should have at least one bpm change at time 0");

        // it's more clear this way
        if (keys.Length == 1)
            return time.ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;

        for (int i = 0; i < keys.Length; i++) {
            double key = keys[i];
            // the last key
            // treat this case similarly to when there's only one key
            if (i == keys.Length - 1)
                return y + (time - key).ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;

            double nextKey = keys[i + 1];
            // the next kext bpm change will occour only after `time`
            // so this can be trated as if this was the last key
            if (nextKey > time)
                return y + (time - key).ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;

            y += (nextKey - key).ToBeat(judgeline.GetClosestBpm(time)) * ChartGlobals.baseNoteSpeed;
        }

        return y;
    }

    public override void _Process(double delta) {
        double time = (Time.GetTicksUsec() - timeBegin) / 1000000.0;
        time -= audioDelay;
        if (time < 0)
            return;

        DeltaTime = time - CurrentTime;
        CurrentTime = time;
        AddActiveEvents();
        signals.Clear();
        FlushEvents();
    }

    // add the events that got activated
    private void AddActiveEvents() {
        for (int i = 0; i < inactiveEvents.Count; i++) {
            Event @event = inactiveEvents[i];
            if (@event.Strart.IsTriggered(this)) {
                activeEvents.Add(@event);
                inactiveEvents.RemoveAt(i);
                i--;
            }
        }
    }

    // NOTE: flush is the correct term here, right?
    private void FlushEvents() {
        for (int i = 0; i < activeEvents.Count; i++) {
            Event @event = activeEvents[i];
            // event just got added
            // NOTE: should this also let
            // update to run?
            if (!@event.active) {
                @event.active = true;
                @event.Strart.InvokeTrigger();
                @event.executionCount = 0;
            }

            if (@event.End.IsTriggered(this)) {
                @event.active = false;
                @event.End.InvokeTrigger();
                activeEvents.RemoveAt(i);
                i--;
                continue; // don't let update run once more
            }

            @event.Update();
            @event.executionCount++;
        }
    }

    public void RegisterEvent(Event @event) {
        inactiveEvents.Add(@event);
    }

    public NativeObject ToObject() {
        return new NativeObjectBuilder(this)
        .AddConstantValue("platform", (int)Platform)
        .AddConstantValue("groups", rootGroup.ToObject()) // should not change
        .AddCallable("add_event", args => {
            if (args.Length == 0)
                throw new ArgumentException("insufficient arguments");

            if (args[0].NativeValue is not Event @event)
                throw new ArgumentException("first argument needs to be an event");

            ChartContext.AddEvent(this, @event);
        })
        .Build();
    }
}