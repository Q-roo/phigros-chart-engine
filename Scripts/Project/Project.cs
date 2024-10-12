using System.Diagnostics;
using System.IO;
using Godot;
using LanguageExt;
using FileAccess = Godot.FileAccess;

namespace PCE.Editor;

public class Project {
    public class ProjectBuilder(string name) {
        private readonly Project project = new(name);

        public Project Build() {
            return project;
        }

        public Either<ProjectBuilder, Error> GenerateFiles() {
            if (DirAccess.DirExistsAbsolute(project.fullPath))
                return Error.AlreadyExists;

            foreach (string directory in new string[] { project.editorDataPath, project.assetsPath }) {
                Error error = DirAccess.MakeDirRecursiveAbsolute(directory);

                if (error != Error.Ok)
                    return error;
            }

            foreach (string file in new string[] { project.undoRedoPath, project.logsPath, project.chartPath }) {
                using FileAccess access = FileAccess.Open(file, FileAccess.ModeFlags.WriteRead);

                if (access is null)
                    return FileAccess.GetOpenError();
            }

            return this;
        }

        public Either<ProjectBuilder, Error> CopyAudio(string path) {
            Error error = DirAccess.CopyAbsolute(path, $"{project.fullPath}/music{Path.GetExtension(path)}");
            if (error != Error.Ok)
                return error;

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
    public string AudioPath {
        get {
            string basePath = fullPath + "/music";
            string wavPath = basePath + ".wav";
            string oggPath = basePath + ".ogg";
            string mp3Path = basePath + ".mp3";

            if (FileAccess.FileExists(wavPath))
                return wavPath;

            if (FileAccess.FileExists(oggPath))
                return oggPath;

            if (FileAccess.FileExists(mp3Path))
                return mp3Path;

            throw new FileNotFoundException("audio file not found");
        }
    }

    // assets
    private AudioStream _audio;
    public AudioStream Audio {
        get {
            _audio ??= LoadAudio();
            return _audio;
        }
    }
    // TODO: load assets from meta
    private Project(string name) {
        this.name = name;
        fullPath = ProjectPathBase + "/" + name;
        editorDataPath = fullPath + "/.pceproject";
        assetsPath = fullPath + "/assets";
        undoRedoPath = editorDataPath + "/.undoredo";
        logsPath = editorDataPath + "/.logs";
        chartPath = fullPath + "/project.chartbuild";
    }

    public static Either<Project, Error> Open(string name) {
        Project project = new(name);

        using DirAccess access = DirAccess.Open(project.fullPath);

        if (access is not null)
            return project;

        return DirAccess.GetOpenError();
    }

    public static ProjectBuilder Create(string name) {
        return new(name);
    }

    public void Open() {
        SelectedProject = this;
        ((SceneTree)Engine.GetMainLoop()).ChangeSceneToFile("res://Scenes/Editor/Editor.tscn");
    }

    private AudioStream LoadAudio() {
        string path = AudioPath;
        switch (Path.GetExtension(path)) {
            case ".wav":
                FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                // FIXME: something is so wrong that it sounds like static from a horror game
                return new AudioStreamWav() { Data = file.GetBuffer((long)file.GetLength()) };
            case ".ogg":
                return AudioStreamOggVorbis.LoadFromFile(path);
            case ".mp3":
                FileAccess access = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                return new AudioStreamMP3() { Data = access.GetBuffer((long)access.GetLength()) };
            default:
                throw new UnreachableException();
        }
    }
}