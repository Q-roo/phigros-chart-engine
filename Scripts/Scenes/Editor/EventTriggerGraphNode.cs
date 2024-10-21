using Godot;

namespace PCE.Editor;

public partial class EventTriggerGraphNode : GraphNode /* ValueContainerGraphNode<EventTrigger> */ {
    private readonly OptionButton dropdown = new();
    // actually slot 2 and 3
    // since these are goung to be offset by the dropdown
    // and a label
    private readonly Label slot0 = new();
    private readonly Label slot1 = new();

    public EventTriggerGraphNode() {
        Title = "trigger";
        AddChild(new Label() { Text = "type" });
        AddChild(dropdown);
        AddChild(slot0);
        AddChild(slot1);
        SetSlotEnabledRight(0, true);

        dropdown.AddItem("begin");
        dropdown.AddItem("end");
        dropdown.AddItem("pause");
        dropdown.AddItem("resume");
        dropdown.AddItem("before");
        dropdown.AddItem("after");
        dropdown.AddItem("exec");
        dropdown.AddItem("once");
        dropdown.AddItem("signal");
        dropdown.AddItem("delay");
        dropdown.AddItem("condtion");
        dropdown.ItemSelected += OnTypeSelected;
    }

    private void OnTypeSelected(long idx) {
        slot0.Text = string.Empty;
        slot1.Text = string.Empty;

        switch (idx) {
            case 4 or 5:
                slot0.Text = "time";
                break;
            case 6:
                slot0.Text = "count";
                break;
            case 8:
                slot0.Text = "name";
                break;
            case 9:
                slot0.Text = "delay";
                slot1.Text = "trigger";
                break;
            case 10:
                slot0.Text = "predicate";
                break;
        }

        SetSlotEnabledLeft(2, slot0.Text != string.Empty);
        SetSlotEnabledLeft(3, slot1.Text != string.Empty);
    }
}