using Godot;
using PCE.Chart;
using PCE.Chart.Util;

namespace PCE.Editor;

public partial class NoteEditor : PanelContainer {
    private OptionButton type;
    private HSlider timeSlider;
    private TripleInput time;
    private HSlider holdTimeSlider;
    private TripleInput holdTime;
    private SpinBox speed;
    private HSlider xOffsetSlider;
    private SpinBox xOffset;
    private CheckBox isAbove;
    private Label test;
    public override void _Ready() {
        type = GetNode<OptionButton>("VBoxContainer/Type/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/OptionButton");
        timeSlider = GetNode<HSlider>("VBoxContainer/Time/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/MarginContainer/HBoxContainer/HSlider");
        time = GetNode<TripleInput>("VBoxContainer/Time/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/MarginContainer/HBoxContainer/TripleInput");
        holdTimeSlider = GetNode<HSlider>("VBoxContainer/HoldTime/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/MarginContainer/HBoxContainer/HSlider");
        holdTime = GetNode<TripleInput>("VBoxContainer/HoldTime/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/MarginContainer/HBoxContainer/TripleInput");
        speed = GetNode<SpinBox>("VBoxContainer/Speed/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/SpinBox");
        xOffsetSlider = GetNode<HSlider>("VBoxContainer/XOffset/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/MarginContainer/HBoxContainer/HSlider");
        xOffset = GetNode<SpinBox>("VBoxContainer/XOffset/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/MarginContainer/HBoxContainer/SpinBox");
        isAbove = GetNode<CheckBox>("VBoxContainer/IsAbove/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/CheckBox");
        test = GetNode<Label>("VBoxContainer/Test/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/Label");

        type.ItemSelected += TypeChanged;
        timeSlider.ValueChanged += TimeSliderChanged;
        time.ValueChanged += TimeInputChanged;
        holdTimeSlider.ValueChanged += HoldTimeSliderChanged;
        holdTime.ValueChanged += HoldTimeInputChanged;
        speed.ValueChanged += SpeedChanged;
        xOffsetSlider.ValueChanged += XOffsetSliderChanged;
        xOffset.ValueChanged += XOffsetInputChanged;
        isAbove.Toggled += IsAboveChanged;

        SetToNote(null);

        EditorContext.Initalized += () => {
            type.AddItem("tap", (int)NoteType.Tap);
            type.AddItem("drag", (int)NoteType.Drag);
            type.AddItem("hold", (int)NoteType.Hold);
            type.AddItem("flick", (int)NoteType.Flick);

            timeSlider.MaxValue = ChartContext.Chart.MusicLengthInSeconds;
            timeSlider.Step = 0;

            holdTimeSlider.MaxValue = ChartContext.Chart.MusicLengthInSeconds;
            holdTimeSlider.Step = 0;

            speed.Step = 0;
            speed.CustomArrowStep = 1;
            speed.AllowGreater = true;
            speed.AllowLesser = true;

            xOffsetSlider.MinValue = -1;
            xOffsetSlider.MaxValue = 1;
            xOffsetSlider.AllowGreater = true;
            xOffsetSlider.AllowLesser = true;
            xOffsetSlider.Step = 0;
            xOffset.MinValue = -1;
            xOffset.MaxValue = 1;
            xOffset.AllowGreater = true;
            xOffset.AllowLesser = true;
            xOffset.Step = 0;
            xOffset.CustomArrowStep = 0.1;

            test.Text = new Triple().ToString();
        };

        ChartContext.FocusedNoteChanged += () => {
            SetToNote(ChartContext.FocusedNote);
        };
    }

    private void SetToNote(Note note) {
        bool isNull = note is null;
        bool isNotNull = !isNull;

        type.Disabled = isNull;
        time.Editable = isNotNull;
        timeSlider.Editable = isNotNull;
        holdTime.Editable = isNotNull;
        holdTimeSlider.Editable = isNotNull;
        speed.Editable = isNotNull;
        xOffset.Editable = isNotNull;
        xOffsetSlider.Editable = isNotNull;
        isAbove.Disabled = isNull;

        if (isNull)
            return;

        type.Select((int)note.type - 1);
        time.SetValueNoSignal(note.time.ToTriple(ChartContext.Chart));
        timeSlider.SetValueNoSignal(note.time);
        holdTime.SetValueNoSignal(note.holdTime.ToTriple(ChartContext.Chart));
        holdTimeSlider.SetValueNoSignal(note.holdTime);
        speed.SetValueNoSignal(note.speed);
        xOffset.SetValueNoSignal(note.XOffset);
        xOffsetSlider.SetValueNoSignal(note.XOffset);
        isAbove.SetPressedNoSignal(note.isAbove);
        test.Text = note.time.ToTriple(ChartContext.Chart).ToString();

        bool isHold = note.type == NoteType.Hold;
        holdTime.Editable = isHold;
        holdTimeSlider.Editable = isHold;
    }

    private void TypeChanged(long _idx) {
        NoteType type = (NoteType)this.type.GetSelectedId();
        ChartContext.FocusedNote.type = type;

        bool isHold = type == NoteType.Hold;
        holdTime.Editable = isHold;
        holdTimeSlider.Editable = isHold;
    }

    private void TimeSliderChanged(double value) {
        ChartContext.FocusedNote.time = timeSlider.Value;
        time.SetValueNoSignal(value.ToTriple(ChartContext.Chart));
    }

    private void TimeInputChanged() {
        double second = time.Value.ToBeat().ToSecond(ChartContext.Chart);
        ChartContext.FocusedNote.time = second;
        timeSlider.SetValueNoSignal(second);
    }

    private void HoldTimeSliderChanged(double value) {
        ChartContext.FocusedNote.holdTime = holdTimeSlider.Value;
        holdTime.SetValueNoSignal(value.ToTriple(ChartContext.Chart));
    }

    private void HoldTimeInputChanged() {
        double second = holdTime.Value.ToBeat().ToSecond(ChartContext.Chart);
        ChartContext.FocusedNote.holdTime = second;
        holdTimeSlider.SetValueNoSignal(second);
    }

    private void SpeedChanged(double value) {
        ChartContext.FocusedNote.speed = (float)speed.Value;
    }

    private void XOffsetSliderChanged(double value) {
        ChartContext.FocusedNote.XOffset = (float)value;
        xOffset.SetValueNoSignal(value);
    }

    private void XOffsetInputChanged(double value) {
        ChartContext.FocusedNote.XOffset = (float)xOffset.Value;
        xOffsetSlider.SetValueNoSignal(value);
    }

    private void IsAboveChanged(bool value) {
        ChartContext.FocusedNote.isAbove = value;
    }
}
