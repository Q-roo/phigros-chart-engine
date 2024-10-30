using Godot;

namespace PCE.Editor;

public partial class GridPositionController : HBoxContainer {
    private SpinBox x;
    private SpinBox y;
    private NotePlacementGrid grid;

    public override void _Ready() {
        x = GetNode<SpinBox>("X");
        y = GetNode<SpinBox>("Y");
        grid = GetNode<NotePlacementGrid>("%NotePlacementGridBackground");

        x.SetValueNoSignal(grid.GridPosition.X);
        y.SetValueNoSignal(grid.GridPosition.Y);
        x.ValueChanged += value => grid.GridPosition = new((float)value, grid.GridPosition.Y);
        y.ValueChanged += value => grid.GridPosition = new(grid.GridPosition.X, (float)value);
        grid.GridPositionChanged += () => {
            x.SetValueNoSignal(grid.GridPosition.X);
            y.SetValueNoSignal(grid.GridPosition.Y);
        };
    }
}
