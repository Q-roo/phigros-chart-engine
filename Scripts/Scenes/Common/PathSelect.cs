using Godot;
using PCE.Util;

namespace PCE.Editor;

public partial class PathSelect : Panel
{
    public delegate void OnFileDialogClose();
    public event OnFileDialogClose FileDialogClose;

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

    private Label titleLabel;
    private Label pathLabel;
    private TextureButton openFileDialogButton;
    private FileDialog fileDialog;

    public sealed override void _Ready()
    {
        titleLabel = GetNode<Label>("HBoxContainer/Title");
        pathLabel = GetNode<Label>("HBoxContainer/Panel/HBoxContainer/Path");
        openFileDialogButton = GetNode<TextureButton>("HBoxContainer/Panel/HBoxContainer/OpenDialog");
        fileDialog = new() {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = true,
            CancelButtonText = "cancel",
            OkButtonText = "select",
            DialogHideOnOk = true,
            DialogCloseOnEscape = true,
            ForceNative = true,
            Title = "Open a file",
            InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen,
            Transient = true,
            TransientToFocused = true,
            Exclusive = true,
        };//GetNode<FileDialog>("FileDialog");

        titleLabel.Text = Title;
        openFileDialogButton.Pressed += OnOpenFileDialogButtonPressed;
        fileDialog.FileSelected += OnFileSelected;
        fileDialog.Canceled += () => FileDialogClose?.Invoke();
        this.GetRootViewport().CallDeferred("add_child", fileDialog);
    }

    private void OnFileSelected(string path)
    {
        SelectedPath = path;
        pathLabel.Text = path;
        FileDialogClose?.Invoke();
    }


    private void OnOpenFileDialogButtonPressed()
    {
        fileDialog.Filters = allowedFileExtensions;
        // fileDialog.GetWindowId();
        // DisplayServer.FileDialogShow("Open A file", "", "", false, DisplayServer.FileDialogMode.OpenFile, allowedFileExtensions, Callable.From((bool status, string[] paths, int filterIndex) => {
        //     GD.Print(paths);
        // }));
        // fileDialog.UseNativeDialog = false;
        fileDialog.Show();
    }

}
