using Godot;

namespace PCE.Editor;

public partial class NotePlacementGridLineController : HBoxContainer
{
    private SpinBox rows;
    private SpinBox columns;
    private NotePlacementGridBackground grid;

    public override void _Ready() {
        rows = GetNode<SpinBox>("Rows");
        columns = GetNode<SpinBox>("Columns");
        grid = GetNode<NotePlacementGridBackground>("%NotePlacementGridBackground");

        rows.SetValueNoSignal(grid.SubBeatCount);
        columns.SetValueNoSignal(grid.Columns);
        rows.ValueChanged += value => grid.SubBeatCount = (int)value;
        columns.ValueChanged += value => grid.Columns = (int)value;
    }
}
