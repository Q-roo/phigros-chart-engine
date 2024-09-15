using Godot;
using System;

namespace PCE.Editor;

public partial class TextInput : Panel
{
    private string _placeholderText;
    [Export]
    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            _placeholderText = value;
            if (input is not null)
                input.PlaceholderText = value;
        }
    }
    private string _title;
    [Export]
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (titleLabel is not null)
                titleLabel.Text = value;
        }
    }
    private string _value;
    [Export]
    public string Value
    {
        get => _value;
        set
        {
            if (input is not null)
                input.Text = value;
            else
                _value = value;
        }
    }

    [GetNode()] private Label titleLabel;
    [GetNode()] private LineEdit input;

    public sealed override void _Ready()
    {
        titleLabel = GetNode<Label>("HBoxContainer/Title");
        input = GetNode<LineEdit>("HBoxContainer/LineEdit");

        input.TextChanged += OnTextChanged;

        titleLabel.Text = Title;
        input.Text = Value;
    }

    private void OnTextChanged(string newText)
    {
        _value = newText;
    }

}
