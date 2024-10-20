using Godot;

namespace PCE.Editor;

public partial class EventNode : GraphNode
{
    public override void _Ready() {
        ClearAllSlots();
        // AddChild(new Label(){ Text = "start trigger" });
        // AddChild(new Label(){ Text = "end trigger" });
        // SetSlotEnabledLeft(0, true);
        // SetSlotColorLeft(0, Colors.AliceBlue);
        // SetSlotEnabledLeft(1, true);
        // SetSlotColorLeft(1, Colors.AliceBlue);
        SetSlot(
            0,
            true, 0, Colors.AliceBlue,
            true, 0, Colors.LightYellow
        );
        SetSlot(
            1,
            true, 0, Colors.AliceBlue,
            true, 0, Colors.SkyBlue
        );
    }
}
