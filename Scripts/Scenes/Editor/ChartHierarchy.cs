using System.Collections.Generic;
using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class ChartHierarchy : Tree {
    private TreeItem root;
    private TreeItem lastSelected;
    private readonly PopupMenu menu;

    public ChartHierarchy() {
        menu = new();
        menu.AddItem("Rename");
        menu.AddItem("Move up");
        menu.AddItem("Move down");
        menu.AddItem("Insert above (TODO)");
        menu.AddItem("Insert below (TODO)");

        menu.IndexPressed += idx => {
            switch (idx) {
                case 0:
                    GetSelected().SetEditable(0, true);
                    EditSelected();
                    break;
                case 1 or 2:
                    // TODO: use MoveTo in chartcontext
                    Node node = GetNodeForItem(GetSelected());
                    Node parent = node.GetParent();
                    int moveToIndex = Mathf.Clamp( node.GetIndex() + (idx == 1 ? -1 : 1), 0, parent.GetChildCount());

                    switch (node) {
                        case Judgeline judgeline:
                            judgeline.MoveTo(moveToIndex);
                            break;
                        case TransformGroup group:
                            group.MoveTo(moveToIndex);
                            break;
                    }

                    Refresh();
                    break;
            }
        };
    }

    public override void _EnterTree() {
        GetViewport().AddChild(menu);
    }

    public override void _Ready() {
        HideRoot = true;
        AllowReselect = true; // click again to start editing the name
        DropModeFlags = (int)DropModeFlagsEnum.OnItem | (int)DropModeFlagsEnum.Inbetween;
        CustomMinimumSize = new(300, 0);
        EditorContext.Initalized += () => {
            EditorContext.Chart.rootGroup.AddJudgeline(new());
            EditorContext.Chart.rootGroup.AddJudgeline(new());
            TransformGroup sub0 = new("s0");
            TransformGroup sub00 = new("s00");
            TransformGroup sub1 = new("s1");
            EditorContext.Chart.rootGroup.AddSubGroup(sub0);
            EditorContext.Chart.rootGroup.AddSubGroup(sub1);
            sub0.AddSubGroup(sub00);
            sub00.AddJudgeline(new());
            sub0.AddJudgeline(new());
            sub1.AddJudgeline(new());
            Refresh();
        };
        ItemSelected += () => {
            TreeItem selected = GetSelected();
            if (lastSelected == selected)
                selected.SetEditable(0, true);

            lastSelected = selected;
            Node node = GetNodeForItem(selected);

            if (node is Judgeline judgeline)
                EditorContext.SelectedJudgeline = judgeline;
        };
        ItemEdited += () => {
            TreeItem edited = GetEdited();
            edited.SetEditable(0, false);
            GetNodeForItem(edited).Name = edited.GetText(0);
        };
    }

    public override void _GuiInput(InputEvent @event) {
        switch (@event) {
            case InputEventKey key:
                if (key.Keycode != Key.F2 || !key.Pressed)
                    return;
                GetSelected().SetEditable(0, true);
                EditSelected();
                break;
            case InputEventMouseButton mouseButton:
                if (mouseButton.ButtonIndex != MouseButton.Right || !mouseButton.Pressed)
                    return;

                TreeItem item = GetItemAtPosition(mouseButton.Position) ?? root;
                SetSelected(item, 0);
                item.Select(0);
                ShowPopup((Vector2I)mouseButton.GlobalPosition);
                break;
        }

    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) {
        return true;
    }

    public override void _DropData(Vector2 atPosition, Variant data) {
        TreeItem item = GetItemAtPosition(atPosition) ?? root;
        Node _group = GetNodeForItem(item);
        TransformGroup targetGroup = _group is Judgeline _judgeline ? _judgeline.parentGroup : (TransformGroup)_group;
        Node node = (Node)data.AsGodotDictionary()["node"];

        // TODO: orderable

        if (node == targetGroup)
            return;

        switch (node) {
            case Judgeline judgeline:
                judgeline.Detach();
                judgeline.AttachTo(targetGroup);
                break;
            case TransformGroup transformGroup:
                transformGroup.Detach();
                transformGroup.AttachTo(targetGroup);
                break;
        }

        Refresh();
    }

    public override Variant _GetDragData(Vector2 atPosition) {
        TreeItem selected = GetSelected();
        Node node = GetNodeForItem(selected);
        Godot.Collections.Dictionary data = new() {
            { "target", selected },
            { "node", node}
        };

        SetDragPreview(new Label() { Text = node.Name });

        return data;
    }

    private void Refresh() {
        Clear();
        root = CreateItem();
        DisplayTransformGroup(root, EditorContext.Chart.rootGroup);
    }

    private void ShowPopup(Vector2I position) {
        bool rootSelected = GetSelected() == root;
        menu.SetItemDisabled(0, rootSelected);
        menu.SetItemDisabled(1, rootSelected);
        menu.SetItemDisabled(2, rootSelected);
        menu.PopupOnParent(new(position, Vector2I.Zero));
    }

    private void DisplayTransformGroup(TreeItem parent, TransformGroup group) {
        foreach (Node child in group.childOrder) {
            TreeItem item = CreateItem(parent);
            item.SetText(0, child.Name);

            // only show judgelines and groups
            if (child is TransformGroup transformGroup)
                DisplayTransformGroup(item, transformGroup);
        }
    }

    private Node GetNodeForItem(TreeItem item) {
        if (item is null)
            return EditorContext.Chart.rootGroup;

        List<int> indices = [];
        while (item != root) {
            indices.Insert(0, item.GetIndex());
            item = item.GetParent();
        }

        // GD.Print(string.Join(", ", indices));

        Node node = EditorContext.Chart.rootGroup;
        foreach (int idx in indices) {
            // indices might contain a last 0th index
            if (node.GetChildCount() == 0)
                break;

            node = node.GetChild(idx);
        }

        return node;
    }
}
