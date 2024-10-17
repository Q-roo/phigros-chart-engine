using Godot;
using LanguageExt;

namespace PCE.Editor;

public partial class NewProjectPopup : PopupPanel {
    Button createButton;
    TextInput nameInput;
    PathSelect musicPath;

    public sealed override void _Ready() {
        createButton = GetNode<Button>("./Control/CreateNewProject");
        nameInput = GetNode<TextInput>("./Control/VBoxContainer/Name");
        musicPath = GetNode<PathSelect>("./Control/VBoxContainer/MusicPath");

        createButton.Pressed += OnCreateButtonPressed;

        // FIXME: popup closes as well when the file dialog closes
        // FIXME: file dialog controls (buttons, scroller) are broken when force native is true
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
