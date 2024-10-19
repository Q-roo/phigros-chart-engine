using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class JudgeLineListController : VBoxContainer
{
    private ItemList list;
    private Button delete;
    Button add;
    TextInput name;
    NumberInput bpm;

    public override void _Ready() {
        list = GetNode<ItemList>("List");
        name = GetNode<TextInput>("HBoxContainer/VBoxContainer/Name");
        bpm = GetNode<NumberInput>("HBoxContainer/VBoxContainer/BPM");
        add = GetNode<Button>("HBoxContainer/Add");
        delete = GetNode<Button>("Delete");

        EditorContext.JudgelineListChanged += RefreshList;
        add.Pressed += AddJudgeline;
        list.ItemSelected += (idx) => EditorContext.SelectedJudgeline = EditorContext.Judgelines[(int)idx / list.MaxColumns];
        delete.Pressed += () => EditorContext.RemoveJudgeline(EditorContext.SelectedJudgeline);
    }

    private void RefreshList() {
        list.Clear();
        foreach (Judgeline judgeline in EditorContext.Judgelines) {
            list.AddItem(judgeline.name);
            list.SetItemMetadata(list.ItemCount - 1, judgeline.InitalBPM);
            list.AddItem($"{judgeline.InitalBPM} BPM", selectable: false);
        }
    }

    private void AddJudgeline() {
        // TODO: scope rules
        EditorContext.AddJudgeline(new(name.Value, (float)bpm.Value, 4000));
    }
}
