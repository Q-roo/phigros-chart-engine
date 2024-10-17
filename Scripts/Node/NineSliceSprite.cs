using Godot;

namespace PCE;

[Tool]
public partial class NineSliceSprite : Node2D {
    [Export] public Texture2D Texture { get; protected set; }
    [Export] private float left = 16;
    [Export] private float right = 16;
    [Export] private float top = 16;
    [Export] private float bottom = 16;

    // icon.svg: 128 x 028
    // t 20
    // b 20
    // l 17
    // r 17
    public override void _Draw() {
        Vector2 size = Texture.GetSize();
        Vector2 offset = size / 2f;

        Rect2 topLeft = new(0, 0, left, top);
        Rect2 bottomLeft = new(0, size.Y - bottom, left, bottom);

        Rect2 topRight = new(size.X - right, 0, right, top);
        Rect2 bottomRight = new(size.X - right, size.Y - bottom, right, bottom);

        Rect2 middleLeft = new(0, top, left, size.Y - top - bottom);
        Rect2 middleRight = new(size.X - right, top, right, size.Y - top - bottom);

        Rect2 topCenter = new(left, 0, size.X - left - right, top);
        Rect2 bottomCenter = new(left, size.Y - bottom, size.X - left - right, bottom);
        Rect2 middleCenter = new(left, top, size.X - left - right, size.Y - top - bottom);

        // DrawRect(new(Vector2.Zero - offset, size), new Color(1, 1, 1, 0.4f));

        // NOTE: only works for y scaling

        // corners
        DrawTextureRectRegion(Texture, new(topLeft.Position - offset, topLeft.Size / Scale), topLeft);
        DrawTextureRectRegion(Texture, new(topRight.Position - offset, topLeft.Size / Scale), topRight);
        DrawTextureRectRegion(Texture, new(new Vector2(0, size.Y - bottom / Scale.Y) - offset, topLeft.Size / Scale), bottomLeft);
        DrawTextureRectRegion(Texture, new(new Vector2(size.X - left, size.Y - bottom / Scale.Y) - offset, topLeft.Size / Scale), bottomRight);
        // center
        DrawTextureRectRegion(Texture, new(new Vector2(left, top) / Scale - offset, size - new Vector2(top + bottom, left + right) / Scale), middleCenter);
        // the rest
        DrawTextureRectRegion(Texture, new(topCenter.Position - offset, topCenter.Size / Scale), topCenter);
        DrawTextureRectRegion(Texture, new(new Vector2(left, size.Y - bottom / Scale.Y) - offset, bottomCenter.Size / Scale), bottomCenter);
        DrawTextureRectRegion(Texture, new(new Vector2(0, top / Scale.Y) - offset, new Vector2(left, size.Y - (top + bottom) / Scale.Y)), middleLeft);
        DrawTextureRectRegion(Texture, new(new Vector2(size.X - right / Scale.X, top / Scale.Y) - offset, new Vector2(right, size.Y - (top + bottom) / Scale.Y)), middleRight);
    }
}