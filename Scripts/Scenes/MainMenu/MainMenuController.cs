using Godot;
using System;

namespace PCE.Editor;

public partial class MainMenuController : Control {
    private Button newProjectButton;
    private Button importButton;
    private Button settingsButton;
    private Button exitButton;
    private ImportFileDialog importFileDialog;
    private NewProjectPopup newProjectWizzard; // well, like a wizzard to be precise
    private ItemList projectList;
    private ProjectPreview projectPreview;

    private string[] projects;

    public sealed override void _Ready() {
        newProjectButton = GetNode<Button>("BG/HSplitContainer/Projects/VBoxContainer/VBoxContainer/New");
        importButton = GetNode<Button>("BG/HSplitContainer/Projects/VBoxContainer/VBoxContainer/Import");
        settingsButton = GetNode<Button>("BG/HSplitContainer/Projects/VBoxContainer/VBoxContainer/Settings");
        exitButton = GetNode<Button>("BG/HSplitContainer/Projects/VBoxContainer/VBoxContainer/Exit");
        importFileDialog = GetNode<ImportFileDialog>("ImportFileDialog");
        newProjectWizzard = GetNode<NewProjectPopup>("NewProjectPopup");
        projectList = GetNode<ItemList>("BG/HSplitContainer/Projects/VBoxContainer/Projects");
        projectPreview = GetNode<ProjectPreview>("BG/HSplitContainer/SelectedProject");

        newProjectButton.Pressed += OnNewProjectPressed;
        importButton.Pressed += OnImportButtonPressed;
        settingsButton.Pressed += OnSettingsButtonPressed;
        exitButton.Pressed += OnExitPressed;

        projectList.ItemSelected += OnProjectSelected;
        importFileDialog.DirSelected += ImportProject;

        projectPreview.ProjectListChanged += RefreshProjects;

        RefreshProjects();
    }

    private void OnProjectSelected(long index) {
        var project = Project.Open(projects[index]);

        project.IfLeft(projectPreview.SetPreview);
        project.IfRight(err => OS.Alert(err.ToString(), "Failed to open project"));
    }

    private void RefreshProjects() {
        projects = GetProjects();

        projectList.Clear();
        foreach (string file in projects) {
            projectList.AddItem(file);
        }
    }

    private static string[] GetProjects() {
        if (!DirAccess.DirExistsAbsolute(Project.ProjectPathBase))
            DirAccess.MakeDirAbsolute(Project.ProjectPathBase);

        return DirAccess.Open(Project.ProjectPathBase).GetDirectories();
    }

    private void ImportProject(string dir) {
        throw new NotImplementedException("TODO: import from " + dir);
    }


    private void OnNewProjectPressed() {
        newProjectWizzard.Show();
    }


    private void OnImportButtonPressed() {
        importFileDialog.Show();
    }


    private void OnSettingsButtonPressed() {
        GetTree().ChangeSceneToFile("res://Scenes/Settings/Settings.tscn");
    }


    private void OnExitPressed() {
        GetTree().Quit();
    }

}
