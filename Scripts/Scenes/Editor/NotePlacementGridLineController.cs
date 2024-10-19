using Godot;

namespace PCE.Editor;

public partial class NotePlacementGridLineController : HBoxContainer
{
    private SpinBox rows;
    private SpinBox columns;
    private NotePlacementGrid grid;

    public override void _Ready() {
        rows = GetNode<SpinBox>("Rows");
        columns = GetNode<SpinBox>("Columns");
        grid = GetNode<NotePlacementGrid>("../../../../TabContainer/Note/HSplitContainer/NotePlacementGrid");

        rows.SetValueNoSignal(grid.SubBeatCount);
        columns.SetValueNoSignal(grid.Columns);
        rows.ValueChanged += value => grid.SubBeatCount = (int)value;
        columns.ValueChanged += value => grid.Columns = (int)value;
    }
}
