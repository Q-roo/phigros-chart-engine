using Godot;

namespace PCE.Editor;

public partial class PathSelect : Panel {
    public delegate void OnFileDialogClose();

    private FileDialog fileDialog;
    public Window forceShow;

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
        fileDialog = GetNode<FileDialog>("FileDialog");

        titleLabel.Text = Title;
        openFileDialogButton.Pressed += OnOpenFileDialogButtonPressed;

        fileDialog.FileSelected += OnFileSelected;
        fileDialog.Canceled += () => OnFileSelected(string.Empty);
    }

    private void OnFileSelected(string path) {
        SelectedPath = path;
        pathLabel.Text = path;
        forceShow?.CallDeferred("show");
    }

    private void OnOpenFileDialogButtonPressed() {
        fileDialog.Filters = allowedFileExtensions;
        forceShow?.Hide(); // prevent error "!windows.has(p_window)" is true
        fileDialog.Show();
    }

}
