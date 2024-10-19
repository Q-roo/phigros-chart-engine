using Godot;

namespace PCE.Editor;

[Tool]
public partial class NumberInput : Panel {
    private readonly HBoxContainer container = new();
    private readonly Label label = new() { Text = "label" };
    public readonly SpinBox spinBox = new();

    [Export]
    public string Title {
        get => label.Text;
        set => label.Text = value;
    }

    [Export]
    public double MinValue {
        get => spinBox.MinValue;
        set => spinBox.MinValue = value;
    }

    [Export]
    public double MaxValue {
        get => spinBox.MaxValue;
        set => spinBox.MaxValue = value;
    }

    [Export]
    public double Step {
        get => spinBox.Step;
        set => spinBox.Step = value;
    }

    [Export]
    public double Value {
        get => spinBox.Value;
        set => spinBox.Value = value;
    }

    [Export]
    public bool AllowGreater {
        get => spinBox.AllowGreater;
        set => spinBox.AllowGreater = value;
    }

    [Export]
    public bool AllowLesser {
        get => spinBox.AllowLesser;
        set => spinBox.AllowLesser = value;
    }

    [Export]
    public bool Editable {
        get => spinBox.Editable;
        set => spinBox.Editable = value;
    }

    public override void _Ready() {
        container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(container);
        container.AddChild(label);
        container.AddChild(spinBox);
        spinBox.SizeFlagsHorizontal |= SizeFlags.Expand;
    }
}
