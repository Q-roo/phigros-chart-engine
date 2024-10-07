using Godot;
using PCE.Chartbuild.Bindings;

namespace PCE.Chart;

public partial class Note : /* NineSliceSprite */ Sprite2D, ICBExposeable {
    public Judgeline parent;
    public override void _Ready() {
        // TODO: note sprites
        // texture = GD.Load<Texture2D>("res://icon.svg");
        Texture = GD.Load<Texture2D>("res://icon.svg");
        QueueRedraw();
    }

    public NativeObject ToObject() {
        return new NativeObject(this);
    }
}