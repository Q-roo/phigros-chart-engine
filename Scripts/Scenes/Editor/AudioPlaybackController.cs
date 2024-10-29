using Godot;

namespace PCE.Editor;

public partial class AudioPlaybackController : HBoxContainer
{
    private Chart.Chart chart;
    private TextureButton SkippBack;
    private TextureButton SkippForward;
    private TextureButton StartStop;
    private TextureButton PauseUnpause;
    private SpinBox skipAmount;
    HSlider time;
    VSlider volume;

    public override void _Ready() {
        chart = GetNode<Chart.Chart>("%ChartRenderer");
        SkippBack = GetNode<TextureButton>("VBoxContainer/HBoxContainer/SkipBack");
        SkippForward = GetNode<TextureButton>("VBoxContainer/HBoxContainer/SkipForward");
        StartStop = GetNode<TextureButton>("VBoxContainer/HBoxContainer/StartStop");
        PauseUnpause = GetNode<TextureButton>("VBoxContainer/HBoxContainer/PauseUnpause");
        skipAmount = GetNode<SpinBox>("VBoxContainer/SkipAmount");
        time = GetNode<HSlider>("VBoxContainer/Time");
        volume = GetNode<VSlider>("Volume");

        volume.MinValue = -80;
        volume.MaxValue = 24;
        volume.SetValueNoSignal(chart.MusicVolume);
        volume.ValueChanged += value => chart.MusicVolume = (float)value;

        time.MaxValue = chart.MusicLengthInSeconds;
        time.ValueChanged += chart.SeekTo;
        SkippBack.Pressed += () => chart.SeekTo(chart.MusicPlaybackPositionInSeconds - skipAmount.Value);
        SkippForward.Pressed += () => chart.SeekTo(chart.MusicPlaybackPositionInSeconds + skipAmount.Value);
        StartStop.Pressed += chart.TogglePlaying;
        PauseUnpause.Pressed += chart.TogglePaused;
    }

    public override void _Process(double delta) {
        time.SetValueNoSignal(chart.MusicPlaybackPositionInSeconds);
    }
}
