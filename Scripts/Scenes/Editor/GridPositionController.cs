using Godot;

namespace PCE.Editor;

public partial class GridPositionController : HBoxContainer {
    private SpinBox x;
    private SpinBox y;
    private NotePlacementGrid grid;

    public override void _Ready() {
        x = GetNode<SpinBox>("X");
        y = GetNode<SpinBox>("Y");
        grid = GetNode<NotePlacementGrid>("../../../../TabContainer/Note/HSplitContainer/NotePlacementGrid");

        x.SetValueNoSignal(grid.GridPosition.X);
        y.SetValueNoSignal(grid.GridPosition.Y);
        x.ValueChanged += value => grid.GridPosition = new((float)value, grid.GridPosition.Y);
        y.ValueChanged += value => grid.GridPosition = new(grid.GridPosition.X, (float)value);
    }
}
