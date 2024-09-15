using Godot;
using System;

namespace PCE.Editor;

public partial class Dropdown : Panel
{
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
    private string[] _options;
    [Export]
    public string[] Options
    {
        get => _options;
        set
        {
            _options = value;
            if (dropdown is not null)
            {
                dropdown.Clear();
                foreach (string option in value)
                    dropdown.AddItem(option);

                if (dropdown.Selected >= value.Length)
                    dropdown.Selected = value.Length - 1;
            }
        }
    }
    public string Selected => dropdown is not null && dropdown.Selected != -1 ? dropdown.GetItemText(dropdown.Selected) : string.Empty;

    [GetNode()] private Label titleLabel;
    [GetNode()] private OptionButton dropdown;

    public sealed override void _Ready()
    {
        titleLabel = GetNode<Label>("HBoxContainer/Title");
        dropdown = GetNode<OptionButton>("HBoxContainer/Dropdown");

        titleLabel.Text = Title;
        foreach (string option in Options)
            dropdown.AddItem(option);

        if (Options.Length != 0)
            dropdown.Selected = 0;
    }
}
