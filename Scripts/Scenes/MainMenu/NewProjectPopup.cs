using Godot;
using LanguageExt;

namespace PCE.Editor;

public partial class NewProjectPopup : PopupPanel {
    Button createButton;
    TextInput nameInput;
    PathSelect musicPath;

    private bool preventHide;

    public sealed override void _Ready() {
        createButton = GetNode<Button>("./Control/CreateNewProject");
        nameInput = GetNode<TextInput>("./Control/VBoxContainer/Name");
        musicPath = GetNode<PathSelect>("./Control/VBoxContainer/MusicPath");

        createButton.Pressed += OnCreateButtonPressed;
        musicPath.FileDialogClose += () => preventHide = true;
        PopupHide += () => {
            if (preventHide) {
                CallDeferred("show");

                preventHide = false;
            }
        };

        // FIXME: file dialog controls (buttons, scroller) are broken when force native is true
        // https://github.com/godotengine/godot/commit/9cbb39f6b25e573cc21389cff295918d1ff4e973 fixes it
        // https://github.com/godotengine/godot/pull/98194
        // TODO: compile the engine from source with this commit and use it
        // current error: https://github.com/godotengine/godot/blob/77dcf97d82cbfe4e4615475fa52ca03da645dbd8/platform/windows/display_server_windows.cpp#L2219
    }

    private void OnCreateButtonPressed() {
        Either<Project, Error> project = Project.Create(nameInput.Value)
            .GenerateFiles()
            .MapLeft(builder => builder.CopyAudio(musicPath.SelectedPath))
            .BindLeft(builder => builder.MapLeft(builder => builder.Build()));

        project.IfLeft(project => project.Open());
        project.IfRight(err => OS.Alert(err.ToString(), "Failed to create project"));
    }

}
