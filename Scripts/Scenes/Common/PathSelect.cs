using Godot;

namespace PCE.Editor;

public partial class PathSelect : Panel {
    public delegate void OnFileDialogClose();
    public event OnFileDialogClose FileDialogClose;

    private readonly Callable OnFileDialogFinish;

    public PathSelect() {
        OnFileDialogFinish = Callable.From((bool status, string[] paths, int filterIndex) => {
            // status is wether it wasn't cancelled
            OnFileSelected(status ? paths[filterIndex] : string.Empty);
            FileDialogClose?.Invoke();
        });
    }

    private string _title;
    [Export]
    public string Title {
        get => _title;
        set {
            _title = value;
            if (titleLabel is not null)
                titleLabel.Text = value;
        }
    }
    [Export] public string[] allowedFileExtensions;
    public string SelectedPath { get; private set; }

    private Label titleLabel;
    private Label pathLabel;
    private TextureButton openFileDialogButton;

    public sealed override void _Ready() {
        titleLabel = GetNode<Label>("HBoxContainer/Title");
        pathLabel = GetNode<Label>("HBoxContainer/Panel/HBoxContainer/Path");
        openFileDialogButton = GetNode<TextureButton>("HBoxContainer/Panel/HBoxContainer/OpenDialog");

        titleLabel.Text = Title;
        openFileDialogButton.Pressed += OnOpenFileDialogButtonPressed;
    }

    private void OnFileSelected(string path) {
        SelectedPath = path;
        pathLabel.Text = path;
    }

    private void OnOpenFileDialogButtonPressed() {
        DisplayServer.FileDialogShow("Open A file", "", "", false, DisplayServer.FileDialogMode.OpenFile, allowedFileExtensions, OnFileDialogFinish);
    }

}
