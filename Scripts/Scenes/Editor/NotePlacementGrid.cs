using System.Diagnostics;
using Godot;
using PCE.Chart;
using PCE.Chart.Util;

namespace PCE.Editor;

public partial class NotePlacementGrid : Panel {
    public delegate void ValueChanged();
    public event ValueChanged GridPositionChanged;

    private const float lineWidth = 5f;
    private const float baseLineOffset = lineWidth / 2f;
    private int _subBeatCount = 3;
    private int _columns = 8;
    private Vector2 _gridPosition;

    private Transform2D UIToNote;

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
            GridPositionChanged?.Invoke();
        }
    }

    private Chart.Chart chart;

    private int VisibleBeats => Mathf.CeilToInt(GetRect().Size.Y / ChartGlobals.DistanceBetweenBeats);

    public NotePlacementGrid() {
        ClipContents = true;
    }

    public override void _Ready() {
        chart = GetNode<Chart.Chart>("%ChartRenderer");
    }

    public override void _Process(double delta) {
        if (chart is null || ChartContext.FocusedJudgeline is null)
            return;

        GridPosition = new(
            GridPosition.X,
            (float)chart.CalculateYPosition(chart.MusicPlaybackPositionInSeconds, ChartContext.FocusedJudgeline)
        );
    }

    private int hoveredIndex = -1;
    private int selectedIndex = -1;
    private Vector2 dragPosition;

    public override void _GuiInput(InputEvent @event) {
        switch (@event) {
            case InputEventMouseButton mouseButton:
                if (mouseButton.ButtonIndex != MouseButton.Left)
                    return;

                if (mouseButton.Pressed) {
                    selectedIndex = GetNoteIndexAtPosition(mouseButton.Position);
                    if (selectedIndex != -1)
                        dragPosition = GetNoteRect(ChartContext.FocusedJudgeline.notes[selectedIndex], ChartContext.FocusedJudgeline).Position;
                } else {
                    Note note = ChartContext.FocusedJudgeline[selectedIndex];
                    Rect2 noteRect = GetNoteRect(note, ChartContext.FocusedJudgeline);
                    noteRect.Position = dragPosition;
                    note.XOffset = GetNoteXOffsetFromRect(noteRect);
                    note.time = GetNoteTimeFromRect(noteRect, note.type != NoteType.Hold);
                    selectedIndex = -1;
                }
                AcceptEvent();
                break;
            case InputEventMouseMotion mouseMotion:
                if ((mouseMotion.ButtonMask & MouseButtonMask.Middle) != 0) {
                    GridPosition += new Vector2(mouseMotion.Relative.X, -mouseMotion.Relative.Y);
                }

                if (selectedIndex == -1)
                    hoveredIndex = GetNoteIndexAtPosition(mouseMotion.Position);
                else {
                    dragPosition += Transform2D.FlipY * mouseMotion.Relative;
                }

                AcceptEvent();
                break;
        }
    }

    public override void _Draw() {
        UpdateTransformMatrices();
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
        int visibleBeatsx2 = 2 * visibleBeats;
        for (int i = 0; i < visibleBeatsx2; i++) {
            float y = i * ChartGlobals.DistanceBetweenBeats - offset.Y;
            DrawHLine(y, Colors.Red);

            for (int j = 1; j <= SubBeatCount; j++) {
                y -= subBeatDistance;
                DrawHLine(y, Colors.Green);
            }
        }

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

        DrawSetTransformMatrix(UIToNote);

        for (int i = 0; i < judgeline.notes.Count; i++) {
            Note note = judgeline.notes[i];
            Rect2 noteRect = GetNoteRect(note, judgeline);

            if (i == selectedIndex) {
                DrawRect(noteRect.Grow(1), Colors.Crimson, false);
                Rect2 dragRect = noteRect;
                dragRect.Position = dragPosition;
                DrawRect(dragRect, Colors.NavyBlue / 1.5f);
            } else if (i == hoveredIndex)
                DrawRect(noteRect.Grow(1), Colors.Cyan, false);

            DrawRect(noteRect, Colors.NavyBlue);
        }
    }

    private void UpdateTransformMatrices() {
        UIToNote = new(Vector2.Right, Vector2.Up, new(0, Size.Y)); // flip y but origin is not at 0,0
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

    private Rect2 GetNoteRect(Note note, Judgeline judgeline) {
        Rect2 rect = GetRect();
        Vector2 center = rect.GetCenter();

        float noteWidth = Columns != 1 ? rect.Size.X / (Columns - 1) : rect.Size.X;
        double height = Mathf.Max(20, ChartContext.Chart.CalculateYPosition(note.holdTime, judgeline));
        float xPosition = center.X + center.X * note.XOffset - noteWidth / 2f;
        double yPosition = ChartContext.Chart.CalculateYPosition(note.time, judgeline);

        if (note.type != NoteType.Hold)
            yPosition -= height / 2f;

        return new(xPosition + GridPosition.X, (float)yPosition - GridPosition.Y, noteWidth, (float)height);
    }

    private float GetNoteXOffsetFromRect(Rect2 noteRect) {
        Rect2 rect = GetRect();
        Vector2 center = rect.GetCenter();

        return (noteRect.Position.X - GridPosition.X - center.X + noteRect.Size.X / 2f) / center.X;
    }

    private double GetNoteTimeFromRect(Rect2 noteRect, bool isNotHold) {
        double distance = noteRect.Position.Y + GridPosition.Y;
        if (isNotHold)
            distance += noteRect.Size.Y / 2f;

        Judgeline judgeline = ChartContext.FocusedJudgeline;
        double[] keys = [.. judgeline.bpmChanges.Keys];
        double accHeight = 0;
        double accTime = 0;

        if (keys.Length == 1)
            return (distance / ChartGlobals.DistanceBetweenBeats).ToSecond(judgeline.bpmChanges[0]);

        for (int i = 0; i < keys.Length; i++) {
            double key = keys[i];
            double range = i == keys.Length - 1 ? double.PositiveInfinity : keys[i + 1] - key;
            double maxHeight = range.ToBeat(judgeline.bpmChanges[key]) * ChartGlobals.DistanceBetweenBeats;

            if (accHeight + maxHeight > distance) {
                double height = distance - accHeight;
                return accTime + (height / ChartGlobals.DistanceBetweenBeats).ToSecond(judgeline.bpmChanges[key]);
            }

            accHeight += maxHeight;
            accTime += range;
        }

        throw new UnreachableException();
    }

    private int GetNoteIndexAtPosition(Vector2 position) {
        Judgeline judgeline = ChartContext.FocusedJudgeline;
        if (judgeline is null)
            return -1;

        for (int i = 0; i < judgeline.notes.Count; i++) {
            Note note = judgeline.notes[i];
            if (GetNoteRect(note, judgeline).HasPoint(UIToNote * position))
                return i;
        }

        return -1;
    }
}
