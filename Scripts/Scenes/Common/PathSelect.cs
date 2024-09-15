using Godot;
using System;

namespace PCE.Editor;

public partial class PathSelect : Panel
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
    [Export] public string[] allowedFileExtensions;
    public string SelectedPath {get; private set; }

    [GetNode("HBoxContainer/Title")] private Label titleLabel;
    [GetNode("HBoxContainer/Panel/HBoxContainer/Path")] private Label pathLabel;
    [GetNode()] private TextureButton openFileDialogButton;
    [GetNode()] private FileDialog fileDialog;

    public sealed override void _Ready()
    {
        titleLabel = GetNode<Label>("HBoxContainer/Title");
        pathLabel = GetNode<Label>("HBoxContainer/Panel/HBoxContainer/Path");
        openFileDialogButton = GetNode<TextureButton>("HBoxContainer/Panel/HBoxContainer/OpenDialog");
        fileDialog = GetNode<FileDialog>("FileDialog");

        titleLabel.Text = Title;
        openFileDialogButton.Pressed += OnOpenFileDialogButtonPressed;
        fileDialog.FileSelected += OnFileSelected;
    }

    private void OnFileSelected(string path)
    {
        SelectedPath = path;
        pathLabel.Text = path;
    }


    private void OnOpenFileDialogButtonPressed()
    {
        fileDialog.Filters = allowedFileExtensions;
        fileDialog.Show();
    }

}
