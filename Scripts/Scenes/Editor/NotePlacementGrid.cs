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
    private const float minNoteHeight = 20;

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
        FocusMode = FocusModeEnum.All;
    }

    public override void _Ready() {
        chart = GetNode<Chart.Chart>("%ChartRenderer");
    }

    public override void _Process(double delta) {
        if (chart is null || ChartContext.FocusedJudgeline is null)
            return;

        GridPosition = new(
            GridPosition.X,
            (float)chart.CalculateYPosition(chart.MusicPlaybackPositionInSeconds)
        );
    }

    private int hoveredIndex = -1;
    private int selectedIndex = -1;
    private Vector2 dragPosition;
    private NoteType placementType = NoteType.Tap;
    private bool holdTimeMove;

    public override void _GuiInput(InputEvent @event) {
        Judgeline judgeline = ChartContext.FocusedJudgeline;

        if (@event is InputEventMouse mouse && GetRect().HasPoint(mouse.Position) && !HasFocus())
            GrabFocus();

        switch (@event) {
            case InputEventKey key:
                if (key.Keycode == Key.Escape && key.Pressed) {
                    selectedIndex = -1;
                    hoveredIndex = -1;
                    AcceptEvent();
                } else if (key.Keycode == Key.Key1) {
                    placementType = NoteType.Tap;
                    AcceptEvent();
                } else if (key.Keycode == Key.Key2) {
                    placementType = NoteType.Drag;
                    AcceptEvent();
                } else if (key.Keycode == Key.Key3) {
                    placementType = NoteType.Hold;
                    AcceptEvent();
                } else if (key.Keycode == Key.Key4) {
                    placementType = NoteType.Flick;
                    AcceptEvent();
                } else if (key.Keycode == Key.Delete || key.Keycode == Key.Backspace) {
                    Note note = null;
                    if (judgeline is not null)
                        if (selectedIndex != -1)
                            note = judgeline.notes[selectedIndex];
                        else if (hoveredIndex != -1)
                            note = judgeline.notes[hoveredIndex];

                    if (note is not null) {
                        note.Detach();
                        if (ChartContext.FocusedNote == note)
                            ChartContext.Focus((Note)null);
                    }

                    selectedIndex = -1;
                    hoveredIndex = -1;
                    AcceptEvent();
                }
                break;
            case InputEventMouseButton mouseButton:
                if (mouseButton.ButtonIndex != MouseButton.Left)
                    return;

                if (judgeline is null) {
                    AcceptEvent();
                    return;
                }

                if (mouseButton.Pressed) {
                    selectedIndex = GetNoteIndexAtPosition(mouseButton.Position);
                    Note note;
                    Rect2 noteRect;

                    if (selectedIndex != -1) {
                        note = judgeline.notes[selectedIndex];
                        noteRect = GetNoteRect(note, judgeline);
                        dragPosition = noteRect.Position;
                    } else {
                        noteRect = new(UIToNote * mouseButton.Position, Vector2.Zero);
                        note = new(placementType, GetNoteTimeFromRect(noteRect, false), GetNoteXOffsetFromRect(noteRect), 1, true, 0);
                        note.AttachTo(judgeline);
                        selectedIndex = judgeline.notes.Count - 1;
                        dragPosition = noteRect.Position;
                        // by making the width of the rect 0, the center of the note will be at the mouse
                        // but now, the drag position has to be offset manually
                        dragPosition.X -= GetNoteDrawWidth() / 2f;
                        dragPosition.Y -= minNoteHeight / 2f;
                    }

                    note.Focus();

                    holdTimeMove = note.type == NoteType.Hold && mouseButton.ShiftPressed;
                    if (holdTimeMove)
                        dragPosition.Y = noteRect.Size.Y;
                } else if (selectedIndex != -1) {
                    Note note = judgeline.notes[selectedIndex];
                    Rect2 noteRect = GetNoteRect(note, judgeline);

                    if (holdTimeMove) {
                        Vector2 size = noteRect.Size;
                        size.Y = Mathf.Max(0, dragPosition.Y);
                        noteRect.Size = size;
                        note.holdTime = GetNoteTimeFromRect(new(noteRect.End, noteRect.Size), false) - note.time;
                    } else {
                        noteRect.Position = dragPosition;
                        note.XOffset = GetNoteXOffsetFromRect(noteRect);
                        note.time = GetNoteTimeFromRect(noteRect, note.type != NoteType.Hold);
                    }

                    // note editor updates when the focused note changes
                    // and this is less of a pain to do than implementing on property list changed
                    note.Focus();

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
                else
                    dragPosition += Transform2D.FlipY * mouseMotion.Relative;

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
            new Note(NoteType.Hold, 2, -1, 1, true, 1).AttachTo(judgeline);
        }

        DrawSetTransformMatrix(UIToNote);

        for (int i = 0; i < judgeline.notes.Count; i++) {
            Note note = judgeline.notes[i];
            Rect2 noteRect = GetNoteRect(note, judgeline);
            Color color = GetNoteColor(note.type);

            DrawRect(noteRect, color);

            if (i == selectedIndex) {
                DrawRect(noteRect.Grow(1), Colors.Crimson, false);
                Rect2 dragRect = noteRect;

                if (holdTimeMove) {
                    Vector2 size = dragRect.Size;
                    size.Y = Mathf.Max(minNoteHeight, dragPosition.Y);
                    dragRect.Size = size;
                } else
                    dragRect.Position = dragPosition;

                DrawRect(dragRect, color / 1.5f);
            } else if (i == hoveredIndex)
                DrawRect(noteRect.Grow(1), Colors.Cyan, false);
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

    private float GetNoteDrawWidth() => Columns != 1 ? Size.X / (Columns - 1) : Size.X;

    private Rect2 GetNoteRect(Note note, Judgeline judgeline) {
        Rect2 rect = GetRect();
        Vector2 center = rect.GetCenter();

        float noteWidth = GetNoteDrawWidth();
        float xPosition = center.X + center.X * note.XOffset - noteWidth / 2f;
        double yPosition = ChartContext.Chart.CalculateYPosition(note.time);
        double height = minNoteHeight;

        if (note.type == NoteType.Hold)
            height = Mathf.Max(minNoteHeight, ChartContext.Chart.CalculateYPosition(note.time + note.holdTime) - yPosition);
        else
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

        return (distance / ChartGlobals.DistanceBetweenBeats).ToSecond(ChartContext.Chart);
    }

    private static Color GetNoteColor(NoteType type) => type switch {
        NoteType.Tap or NoteType.Hold => Colors.NavyBlue,
        NoteType.Drag => Colors.Yellow,
        NoteType.Flick => Colors.Red,
        _ => Colors.Black
    };

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
