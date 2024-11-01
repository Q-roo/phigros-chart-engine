using System.Text;
using Godot;
using PCE.Chart;

namespace PCE;

[GlobalClass]
public partial class TripleInput : LineEdit {
    [Signal]
    public delegate void ValueChangedEventHandler();

    private Triple _value;
    public Triple Value { get => _value; set => SetValue(value, true); }

    public TripleInput() {
        Text = Value.ToString(); // won't emit the text changed signal
        ExpandToTextLength = true;
        Alignment = HorizontalAlignment.Center;
        TextSubmitted += text => SetValue(text, true);
        TextChanged += text => SetValue(text, false);
        // input let's the user input unfinished but valid (e.g: a, a:, a:b, a:b/) string
        // while they are writing it
        // but once they submit it, it get's validated properly
        // treat clicking away as submitting
        FocusExited += () => OnTextSubmitted(Text);

    }

    public void SetValueNoSignal(Triple value) {
        SetValue(value, false);
    }

    private void SetValue(string input, bool isSubmitting) {
        if (string.IsNullOrEmpty(input)) {
            Value = new();
        }

        StringBuilder[] parts = [new(), new(), new()];

        // 0:b/c -> phase 0:phase 1/phase 2
        int phase = 0;

        for (int i = 0; i < input.Length; i++) {
            char c = input[i];
            // only allowed characters are 0-9,:,/
            if (
                !char.IsBetween(c, '0', '9')
                && c != ':'
                && c != '/'
            ) {
                DeleteCharAtCaret();
                return;
            }

            if (c == ':') {
                GD.Print(phase);
                // invalid syntax (e.g.: a::b/c)
                if (phase != 0) {
                    DeleteCharAtCaret();
                    return;
                }

                phase++;
                continue;
            }

            if (c == '/') {
                // invalid syntax (e.g: a/b:c)
                if (phase != 1) {
                    // : got deleted by the user resulting in b/c
                    if (CaretColumn == Text.Length)
                        DeleteCharAtCaret();
                    else
                        Text = Value.ToString();

                    return;
                }
                phase++;
                continue;
            }

            parts[phase].Append(c);
        }

        bool isAEmpty = parts[0].Length == 0;
        bool isBEmpty = parts[1].Length == 0;
        bool isCEmpty = parts[2].Length == 0;

        if (
            isAEmpty
            || isBEmpty
            || isCEmpty
            ) {
            // cases -> still writing
            // a
            // a:b
            if (!isSubmitting)
                if (
                    (!isAEmpty && isBEmpty && isCEmpty)
                    || (!isAEmpty && !isBEmpty)
                ) return;

            // reset it to the last valid value
            Text = Value.ToString();
        }

        // the characters should are valid
        // but the user could still input numbers that are too large
        int barNumber;
        uint numerator;
        uint denominator;

        if (!int.TryParse(parts[0].ToString(), out int _beat)) {
            barNumber = int.MaxValue;
            GD.PushWarning("[part a] ", parts[0], " is too big for int. using ", int.MaxValue);
        } else
            barNumber = _beat;

        if (!uint.TryParse(parts[1].ToString(), out uint _numerator)) {
            numerator = uint.MaxValue;
            GD.PushWarning("[part b] ", parts[1], " is too big for uint. using ", uint.MaxValue);
        } else
            numerator = _numerator;

        if (!uint.TryParse(parts[2].ToString(), out uint _denominator)) {
            denominator = uint.MaxValue;
            GD.PushWarning("[part c] ", parts[2], " is too big for uint. using ", uint.MaxValue);
        } else
            denominator = _denominator;

        Value = new(barNumber, numerator, denominator);
        CaretColumn = Text.Length;
    }

    private void SetValue(Triple value, bool notify) {
        _value = value;
        Text = value.ToString();
        if (notify)
            OnValueChanged();
    }
}