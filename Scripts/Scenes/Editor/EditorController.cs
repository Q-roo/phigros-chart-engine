using Godot;
using System;

namespace PCE.Editor;

public partial class EditorController : Control
{
    [GetNode] private ChartBuildCodeEdit codeEditor;

    public sealed override void _Ready()
    {
        codeEditor = GetNode<ChartBuildCodeEdit>("Panel/TabContainer/Chartbuild/CodeEdit");
        codeEditor.Open(Project.SelectedProject);
    }
}
