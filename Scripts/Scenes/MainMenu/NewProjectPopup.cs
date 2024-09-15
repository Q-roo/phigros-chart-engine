using DotNext;
using Godot;
using PCE.Util;
using System;

namespace PCE.Editor;

public partial class NewProjectPopup : PopupPanel
{
    [GetNode("./Control/CreateNewProject")] Button createButton;
    [GetNode("./Control/VBoxContainer/Name")] TextInput nameInput;

    public sealed override void _Ready()
    {
        createButton = GetNode<Button>("./Control/CreateNewProject");
        nameInput = GetNode<TextInput>("./Control/VBoxContainer/Name");

        createButton.Pressed += OnCreateButtonPressed;
    }

    private void OnCreateButtonPressed()
    {
        Result<Project, Error> project = Project.Create(nameInput.Value)
            .GenerateFiles()
            .AndThen(builder => builder.Build());

        if (project)
            project.Value.Open();
        else
            OS.Alert(project.Error.ToString(), "Failed to create project");
    }

}
