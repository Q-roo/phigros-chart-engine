using System.Text.RegularExpressions;
using Godot;
using PCE.Chart;

namespace PCE;

[GlobalClass]
public partial class TripleInput : LineEdit {
    [Signal]
    public delegate void ValueChangedEventHandler();

    private static readonly Regex tripleValidationRegex = GetValidationRegex();

    // it would be 0, 0, 0 insetad of 0, 0, 1 otherwise
    private Triple _value = Triple.Default;
    public Triple Value { get => _value; set => SetValue(value, true); }

    public TripleInput() {
        Text = Value.ToString(); // won't emit the text changed signal
        ExpandToTextLength = true;
        Alignment = HorizontalAlignment.Center;
        TextSubmitted += Submit;
        TextChanged += RemoveIllegalCharacters;
        // TextChanged += text => SetValue(text, false);
        // input let's the user input anything with valid characters (e.g: a, a:, a:b, a:b/)
        // (it still prevents cases like //, /:, ...etc)
        // while they are writing it
        // but once they submit it, it get's validated properly
        // treat clicking away as submitting
        FocusExited += () => OnTextSubmitted(Text);

    }

    public void SetValueNoSignal(Triple value) {
        SetValue(value, false);
    }

    private void Submit(string input) {
        if (string.IsNullOrEmpty(input)) {
            Value = Triple.Default;
            return;
        }

        Match match = tripleValidationRegex.Match(input);
        if (!match.Success) {
            Value = Triple.Default;
            return;
        }

        // groups[0] is the matched string

        if (!int.TryParse(match.Groups[1].ToString(), out int barNumber)) {
            barNumber = int.MaxValue;
            GD.PushWarning("[part a] ", match.Groups[1], " is too big for int. using ", int.MaxValue);
        }

        if (!uint.TryParse(match.Groups[2].ToString(), out uint numerator)) {
            numerator = uint.MaxValue;
            GD.PushWarning("[part b] ", match.Groups[2], " is too big for uint. using ", uint.MaxValue);
        }

        if (!uint.TryParse(match.Groups[3].ToString(), out uint denominator)) {
            denominator = uint.MaxValue;
            GD.PushWarning("[part c] ", match.Groups[3], " is too big for uint. using ", uint.MaxValue);
        }

        Value = new(barNumber, numerator, denominator);
    }

    private void RemoveIllegalCharacters(string text) {
        if (string.IsNullOrEmpty(text))
            return;

        bool colonFound = false;
        bool slashFound = false;

        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            bool isColon = c == ':';
            bool isSlash = c == '/';

            if (
                (isColon && colonFound) // cases like ::
                || (isSlash && (slashFound || !colonFound)) // cases like // and /:
                || (!char.IsAsciiDigit(c) && !isColon && !isSlash) // illegal character
            )
                DeleteText(i, i + 1);
            else if (isColon)
                colonFound = true;
            else if (isSlash)
                slashFound = true;
        }
    }

    private void SetValue(Triple value, bool notify) {
        _value = value;
        Text = Value.ToString();

        if (notify)
            OnValueChanged();
    }

    [GeneratedRegex(@"(\d+)(?::(\d+)/(\d+))")]
    private static partial Regex GetValidationRegex();
}