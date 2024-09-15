using Godot;
using System;
using System.Collections.Generic;

namespace PCE.Editor;

public partial class ProjectPreview : Panel
{
    [Signal]
    public delegate void ProjectListChangedEventHandler();

    private Project project;
    [GetNode] private TextureRect jacketPreview;
    [GetNode("./VBoxContainer/Edit")] private Button editButton;
    [GetNode("./VBoxContainer/Delete")] private Button deleteButton;
    [GetNode("./VBoxContainer/Export")] private Button exportButton;

    public sealed override void _Ready()
    {
        jacketPreview = GetNode<TextureRect>("./TextureRect");
        editButton = GetNode<Button>("./VBoxContainer/Edit");
        deleteButton = GetNode<Button>("./VBoxContainer/Delete");
        exportButton = GetNode<Button>("./VBoxContainer/Export");

        editButton.Pressed += OnEditButtonPressed;
        deleteButton.Pressed += OnDeleteButtonPressed;
        exportButton.Pressed += OnExportButtonPressed;
    }

    private void OnExportButtonPressed()
    {
        OS.Alert("TODO", "export to phira, rpe, phigros");
    }


    private void OnDeleteButtonPressed()
    {
        List<string> emptyDirectories = new();
        List<string> directories = [project.fullPath];

        while (directories.Count > 0)
        {
            string path = directories[0];
            GD.Print(path);
            DirAccess directory = DirAccess.Open(path);

            if (directory == null)
            {
                OS.Alert("Failed to delete project", DirAccess.GetOpenError().ToString());
                return;
            }

            directories.RemoveAt(0);
            foreach (string dir in directory.GetDirectories())
            {
                directories.Add(path + "/" + dir);
            }

            foreach (string file in directory.GetFiles())
            {
                Error error = directory.Remove(path + "/" +file);

                if (error != Error.Ok)
                {
                    OS.Alert("Failed to delete project", DirAccess.GetOpenError().ToString());
                    return;
                }
            }

            emptyDirectories.Add(path);
        }

        for (int i = emptyDirectories.Count - 1; i >= 0; i--)
        {
            string path = emptyDirectories[i];

            Error error = DirAccess.RemoveAbsolute(path);

            if (error != Error.Ok)
            {
                OS.Alert("Failed to delete project", DirAccess.GetOpenError().ToString());
                return;
            }
        }

        EmitSignal(SignalName.ProjectListChanged);
    }


    private void OnEditButtonPressed()
    {
        if (project is null)
        {
            OS.Alert("selected project is not set", "Cannot open project");
            return;
        }

        project.Open();
    }


    public void SetPreview(Project project)
    {
        this.project = project;
        // jacketPreview.Texture = GD.Load<Texture2D>(""); // TODO
        // TODO: songPreview

    }
}
