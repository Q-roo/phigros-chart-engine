using System.Collections.Generic;
using Godot;
using PCE.Chart.Util;
using PCE.Chartbuild.Bindings;
using PCE.Chartbuild.Runtime;

namespace PCE.Chart;

public partial class Note : /* NineSliceSprite */ Sprite2D, ICBExposeable {
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

    public override void _Ready() {
        // TODO: note sprites
        // texture = GD.Load<Texture2D>("res://icon.svg");
        Texture = GD.Load<Texture2D>("res://icon.svg");
        GetTree().Root.SizeChanged += UpdateXOffset;
        UpdateXOffset();
    }

    public override void _Process(double delta) {
        Vector2 position = Position;
        // TODO: add the note speed into the equation as well
        position.Y += (float)(delta.ToBeat(Parent.GetCurrentBpm()) * ChartGlobals.baseNoteSpeed);
        Position = position;
    }

    public override void _Draw() {
        Vector2 xy = Texture.GetSize() / 2f;
        DrawLine(new(-xy.X, xy.Y), new(xy.X, xy.Y), new(1, 1, 1), 2);
        DrawCircle(Vector2.Down * xy, 5, new(1, 1, 1));
    }

    private void UpdateXOffset() {
        Vector2 position = Position;
        position.X = GetViewportRect().Size.X * XOffset;
        Position = position;
    }

    public NativeObject ToObject() {
        return new NativeObjectBuilder(this)
        .AddChangeableProperty("position", () => XOffset, value => XOffset = value)
        .Build();
    }
}