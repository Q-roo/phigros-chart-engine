using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class NoteEditor : PanelContainer {
    private OptionButton type;
    private HSlider timeSlider;
    private SpinBox time;
    private HSlider holdTimeSlider;
    private SpinBox holdTime;
    private SpinBox speed;
    private HSlider xOffsetSlider;
    private SpinBox xOffset;
    private CheckBox isAbove;

    public override void _Ready() {
        type = GetNode<OptionButton>("VBoxContainer/Type/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/OptionButton");
        timeSlider = GetNode<HSlider>("VBoxContainer/Time/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/HBoxContainer/HSlider");
        time = GetNode<SpinBox>("VBoxContainer/Time/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/HBoxContainer/SpinBox");
        holdTimeSlider = GetNode<HSlider>("VBoxContainer/HoldTime/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/HBoxContainer/HSlider");
        holdTime = GetNode<SpinBox>("VBoxContainer/HoldTime/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/HBoxContainer/SpinBox");
        speed = GetNode<SpinBox>("VBoxContainer/Speed/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/SpinBox");
        xOffsetSlider = GetNode<HSlider>("VBoxContainer/XOffset/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/HBoxContainer/HSlider");
        xOffset = GetNode<SpinBox>("VBoxContainer/XOffset/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/HBoxContainer/SpinBox");
        isAbove = GetNode<CheckBox>("VBoxContainer/IsAbove/MarginContainer/HBoxContainer/MarginContainer/PanelContainer/CheckBox");

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
            time.MaxValue = ChartContext.Chart.MusicLengthInSeconds;
            time.Step = 0;
            time.CustomArrowStep = 1;

            holdTimeSlider.MaxValue = ChartContext.Chart.MusicLengthInSeconds;
            holdTimeSlider.Step = 0;
            holdTime.MaxValue = ChartContext.Chart.MusicLengthInSeconds;
            holdTime.Step = 0;
            holdTime.CustomArrowStep = 1;

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

        if (isNull)
            return;

        type.Select((int)note.type - 1);
        time.SetValueNoSignal(note.time);
        timeSlider.SetValueNoSignal(note.time);
        holdTime.SetValueNoSignal(note.holdTime);
        holdTimeSlider.SetValueNoSignal(note.holdTime);
        speed.SetValueNoSignal(note.speed);
        xOffset.SetValueNoSignal(note.XOffset);
        xOffsetSlider.SetValueNoSignal(note.XOffset);
        isAbove.SetPressedNoSignal(note.isAbove);

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
        time.SetValueNoSignal(value);
    }

    private void TimeInputChanged(double value) {
        ChartContext.FocusedNote.time = time.Value;
        timeSlider.SetValueNoSignal(value);
    }

    private void HoldTimeSliderChanged(double value) {
        ChartContext.FocusedNote.holdTime = holdTimeSlider.Value;
        holdTime.SetValueNoSignal(value);
    }

    private void HoldTimeInputChanged(double value) {
        ChartContext.FocusedNote.holdTime = holdTime.Value;
        holdTimeSlider.SetValueNoSignal(value);
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
