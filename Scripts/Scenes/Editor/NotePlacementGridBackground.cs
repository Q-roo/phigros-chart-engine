using Godot;
using PCE.Chart;

namespace PCE.Editor;

public partial class NotePlacementGridBackground : Panel {
    public delegate void ValueChanged();
    public event ValueChanged GridPositionChanged;

    private partial class NoteDisplay : Node2D {
        public NotePlacementGridBackground background;
        public override void _Draw() {
            DrawNotes();
        }

        private void DrawNotes() {
        Judgeline judgeline = ChartContext.FocusedJudgeline;
        if (judgeline is null)
            return;

        // test
        if (judgeline.notes.Count == 0) {
            new Note(NoteType.Tap, 0, 0, 1, true, 0).AttachTo(judgeline);
            new Note(NoteType.Tap, 0.5, 0.5f, 1, true, 0).AttachTo(judgeline);
            new Note(NoteType.Tap, 1, 1, 1, true, 0).AttachTo(judgeline);
            new Note(NoteType.Tap, 2, -1, 1, true, 1).AttachTo(judgeline);
        }

        Rect2 rect = background.GetRect();
        Vector2 rectCenter = rect.GetCenter();
        float noteWidth = background.Columns != 1 ? rect.Size.X / (background.Columns - 1) : rect.Size.X;

        foreach (Note note in judgeline.notes) {
            double height = Mathf.Max(20, ChartContext.Chart.CalculateYPosition(note.holdTime, judgeline));
            double yPosition = -ChartContext.Chart.CalculateYPosition(note.time, judgeline) - height + rect.Size.Y;
            float xPosition = rectCenter.X + rectCenter.X * note.XOffset - noteWidth / 2f;
            Vector2 position = new(xPosition, (float)yPosition);
            DrawRect(new(position, noteWidth, (float)height), Colors.NavyBlue);
        }
    }
    }

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
            display.Position = new(value.X, -value.Y);
            GridPositionChanged?.Invoke();
        }
    }

    private int VisibleBeats => Mathf.CeilToInt(GetRect().Size.Y / ChartGlobals.DistanceBetweenBeats);
    private readonly NoteDisplay display;

    public NotePlacementGridBackground() {
        ClipContents = true;
        display = new() {
            background = this
        };
        AddChild(display);
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

        display.QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is not InputEventMouseMotion mouse || (mouse.ButtonMask & MouseButtonMask.Middle) == 0)
            return;

        GridPosition += new Vector2(mouse.Relative.X, -mouse.Relative.Y);
        AcceptEvent();
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
