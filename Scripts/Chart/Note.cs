using Godot;
using PCE.Chart.Util;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public partial class Note : NineSliceSprite, ICBExposeable {
    // all this just so I won't have to check wether the parent is null in _Proccess
    private Judgeline _parent;
    public Judgeline Parent {
        get => _parent;
        set {
            _parent = value;
            SetProcess(value is not null);
        }
    }
    public NoteType type;
    // it's in seconds
    public double time;
    public double holdTime;
    public float speed;
    public bool isAbove;
    // percentage value where 1 = 100%
    // use the viewport width
    // because judgeline width can be changed regularly
    // between -1 and 1
    // though, nothing stops anyone from using larger or smaller values
    private float _xOffset;
    public float XOffset {
        get => _xOffset;
        set {
            _xOffset = value;
            UpdateXOffset();
        }
    }

    public Note(NoteType type, double time, float xOffset, float speed, bool isAbove, double holdTime) {
        Parent = null;
        this.type = type;
        this.time = time;
        _xOffset = xOffset;
        this.speed = speed;
        this.isAbove = isAbove;
        this.holdTime = holdTime;
    }

    public override bool _Set(StringName property, Variant value) {
        bool success = base._Set(property, value);
        if (success)
            NotifyPropertyListChanged();
        
        GD.Print($"[success={success}]set \"{property}\" to {value}");

        return success;
    }

    public override void _Ready() {
        // TODO: note sprites
        Texture = GD.Load<Texture2D>("res://icon.svg");
        // Texture = GD.Load<Texture2D>("res://icon.svg");
        GetTree().Root.SizeChanged += UpdateXOffset;
        UpdateXOffset();
    }

    public override void _Process(double delta) {
        Vector2 position = Position;
        // TODO: add the note speed into the equation as well
        position.Y += (float)(delta.ToBeat(ChartContext.Chart.CurrentBPM) * ChartGlobals.baseNoteSpeed);
        Position = position;
    }

    public override void _Draw() {
        base._Draw();
        // Vector2 xy = Texture.GetSize() / 2f;
        Vector2 xy = Texture.GetSize() / 2f;
        // this should be independent of the note scale
        DrawLine(new(-xy.X, xy.Y), new(xy.X, xy.Y), new(1, 1, 1), 2 / Scale.Y);

        // draw a 5x5 rectangle independent of the current scale
        // and offset it correctly so it's at the center of the line
        // NOTE: currently, only the y value has a chance to be scaled
        DrawRect(new Rect2(Vector2.Down * new Vector2(xy.X, xy.Y - 5f / Scale.Y / 2f), new Vector2(5, 5) / Scale), new(1, 1, 1));
    }

    private void UpdateXOffset() {
        Vector2 position = Position;
        position.X = GetViewportRect().Size.X / 2f * XOffset;
        Position = position;
    }

    public NativeObject ToObject() {
        return new NativeObjectBuilder(this)
        .AddChangeableProperty("position", () => XOffset, value => XOffset = value)
        .Build();
    }
}