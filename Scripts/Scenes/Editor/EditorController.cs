using System.Linq;
using Godot;

namespace PCE.Editor;

public partial class EditorController : Control {
    [GetNode] private ChartBuildCodeEdit codeEditor;

    public sealed override void _Ready() {
        codeEditor = GetNode<ChartBuildCodeEdit>("VBoxContainer/TabContainer/Chartbuild/VBoxContainer/CodeEdit");
        codeEditor.Open(Project.SelectedProject);

        // is this a hacky solution? yes
        // am I too lazy to make a custom tab container? also yes
        TabContainer container = GetNode<TabContainer>("VBoxContainer/TabContainer");
        TabBar tabs = GetNode<TabBar>("VBoxContainer/Statusbar/HBoxContainer/Tabs");
        float minWidth = 0;

        foreach (Node tab in container.GetChildren()) {
            tabs.AddTab(tab.Name);
            minWidth += tabs.GetTabRect(tabs.TabCount - 1).Size.X;
        }

        tabs.TabSelected += (idx) => container.CurrentTab = (int)idx;
        tabs.CustomMinimumSize = new(minWidth, 0);
    }
}
