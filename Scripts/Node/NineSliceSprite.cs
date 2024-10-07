using Godot;

namespace PCE;

[Tool]
public partial class NineSliceSprite : Node2D {
    [Export] Texture2D texture;
    [Export] float left = 17;
    [Export] float right = 17;
    [Export] float top = 20;
    [Export] float bottom = 20;

    // icon.svg: 128 x 028
    // t 20
    // b 20
    // l 17
    // r 17
    public override void _Draw() {
        Vector2 size = texture.GetSize();

        Rect2 topLeft = new(0, 0, left, top);
        Rect2 bottomLeft = new(0, size.Y - bottom, left, bottom);

        Rect2 topRight = new(size.X - right, 0, right, top);
        Rect2 bottomRight = new(size.X - right, size.Y - bottom, right, bottom);

        Rect2 middleLeft = new(0, top, left, size.Y - top - bottom);
        Rect2 middleRight = new(size.X - right, top, right, size.Y - top - bottom);

        Rect2 topCenter = new(left, 0, size.X - left - right, top);
        Rect2 bottomCenter = new(left, size.Y - bottom, size.X - left - right, bottom);
        Rect2 middleCenter = new(left, top, size.X - left - right, size.Y - top - bottom);

        DrawRect(new(Vector2.Zero, size), new Color(1, 1, 1));

        Rect2 rect = new(topLeft.Position, topLeft.Size / Scale);

        DrawTextureRectRegion(texture, rect, topLeft); // top-left
        rect = new(rect.Position.X + left / 2, rect.Position.Y, topCenter.Size.X + rect.Size.X * 2, topCenter.Size.Y / Scale.Y);
        DrawTextureRectRegion(texture, rect, topCenter); // top-center
        rect = new(topRight.Size.X / 2 + rect.Size.X, topRight.Position.Y, topLeft.Size / Scale);
        DrawTextureRectRegion(texture, rect, topRight); // top-right
        rect = new(rect.Position.X, rect.Size.Y, middleRight.Size.X / Scale.X, middleRight.Size.Y + rect.Size.Y + bottomRight.Size.Y / Scale.Y);
        DrawTextureRectRegion(texture, rect, middleRight); // right-center
        rect = new(rect.Position.X - middleCenter.Size.X - (left + right) / 2, rect.Position.Y, size.X - (left + right) / 2, size.Y - (top + bottom) / 2);
        DrawTextureRectRegion(texture, rect, middleCenter); // middle-center
        rect = new(rect.Position.X - left / 2, rect.Position.Y, middleLeft.Size.X / Scale.X, rect.Size.Y);
        DrawTextureRectRegion(texture, rect, middleLeft); // left-center
        rect = new(rect.Position.X, rect.Position.Y + rect.Size.Y, rect.Size.X, bottomLeft.Size.X / Scale.X);
        DrawTextureRectRegion(texture, rect, bottomLeft); // bottom-left
        DrawTextureRectRegion(texture, rect, bottomRight); // bottom-right
        DrawTextureRectRegion(texture, rect, bottomCenter); // bottom-center
    }
}