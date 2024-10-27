using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class NotePlacementGridBackground : Panel {
    private const float lineWidth = 5f;
    private const float baseLineOffset = lineWidth / 2f;
    private int _subBeatCount = 3;
    private int _columns = 8;
    private Vector2 _gridPosition;
    public int SubBeatCount {
        get => _subBeatCount;
        set {
            _subBeatCount = value;
            QueueRedraw();
        }
    }
    public int Columns {
        get => _columns;
        set {
            _columns = value;
            QueueRedraw();
        }
    }
    public Vector2 GridPosition {
        get => _gridPosition;
        set {
            _gridPosition = value;
            QueueRedraw();
        }
    }

    private int VisibleBeats => Mathf.CeilToInt(GetRect().Size.Y / ChartGlobals.DistanceBetweenBeats);

    public NotePlacementGridBackground() {
        ClipContents = true;
    }

    public override void _Draw() {
        Rect2 rect = GetRect();
        Vector2 distance = rect.Size / new Vector2(Columns - 1, SubBeatCount);
        // do not add base line offset to x
        // or the last line will be drawn outside by line width
        Vector2 offset = new(Mathf.Wrap(GridPosition.X, rect.Position.X, rect.End.X), Mathf.Wrap(GridPosition.Y, rect.Position.Y, rect.End.Y) + baseLineOffset);
        float subBeatDistance = ChartGlobals.DistanceBetweenBeats / (SubBeatCount + 1f);

        // DrawRect(rect, Colors.DimGray);
        if (Columns == 1)
            DrawVLine(rect.Size.X / 2f + offset.X, Colors.Yellow);

        for (int i = -Columns; i < Columns; i++)
            DrawVLine(distance.X * i + offset.X, Colors.Yellow);

        int visibleBeats = VisibleBeats;
        for (int i = -visibleBeats; i < visibleBeats; i++) {
            float y = i * ChartGlobals.DistanceBetweenBeats + offset.Y;
            DrawHLine(y, Colors.Red);

            for (int j = 1; j <= SubBeatCount; j++) {
                y += subBeatDistance;
                DrawHLine(y, Colors.Green);
            }
        }
    }

    private void DrawVLine(float x, Color color) {
        Rect2 rect = GetRect();
        color /= 1.5f;
        DrawLine(new(x, rect.Position.Y), new(x, rect.End.Y), color, lineWidth);
    }
    private void DrawHLine(float y, Color color) {
        Rect2 rect = GetRect();
        color /= 1.5f;
        // flip y
        y = rect.Size.Y - y;
        DrawLine(new(rect.Position.X, y), new(rect.End.X, y), color, lineWidth);
    }
}
