using Godot;
using DotNext;
using PCE.Util;

namespace PCE.Editor;

public class Project
{
    public class ProjectBuilder(string name) : Builder<Project>
    {
        private readonly Project project = new(name);

        public Project Build()
        {
            return project;
        }

        public Result<Builder<Project>, Error> GenerateFiles()
        {
            if (DirAccess.DirExistsAbsolute(project.fullPath))
                return new(Error.AlreadyExists);

            foreach (string directory in new string[] { project.editorDataPath, project.assetsPath })
            {
                Error error = DirAccess.MakeDirRecursiveAbsolute(directory);

                if (error != Error.Ok)
                    return new(error);
            }

            foreach (string file in new string[] { project.undoRedoPath, project.logsPath, project.chartPath })
            {
                using FileAccess access = FileAccess.Open(file, FileAccess.ModeFlags.WriteRead);

                if (access is null)
                    return new(FileAccess.GetOpenError());
            }

            return this;
        }
    }

    public const string ProjectPathBase = "user://projects";

    public static Project SelectedProject { get; private set; }

    public readonly string name;

    // directories
    public readonly string fullPath;
    public readonly string editorDataPath;
    public readonly string assetsPath;

    // files
    public readonly string undoRedoPath;
    public readonly string logsPath;
    public readonly string chartPath;
    // TODO: load assets from meta
    private Project(string name)
    {
        this.name = name;
        fullPath = ProjectPathBase + "/" + name;
        editorDataPath = fullPath + "/.pceproject";
        assetsPath = fullPath + "/assets";
        undoRedoPath = editorDataPath + "/.undoredo";
        logsPath = editorDataPath + "/.logs";
        chartPath = fullPath + "/project.chartbuild";
    }

    public static Result<Project, Error> Open(string name)
    {
        Project project = new(name);

        using DirAccess access = DirAccess.Open(project.fullPath);

        if (access is not null)
            return project;

        return new(DirAccess.GetOpenError());
    }

    public static ProjectBuilder Create(string name)
    {
        return new(name);
    }

    public void Open()
    {
        SelectedProject = this;
        ((SceneTree)Engine.GetMainLoop()).ChangeSceneToFile("res://Scenes/Editor/Editor.tscn");
    }
}