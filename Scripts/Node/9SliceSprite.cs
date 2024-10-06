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
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 0, l, t));
        // DrawHold();
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 0, 17, 128)); // left
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(128-17, 0, 17, 128)); // right
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 0, 128, 20)); // top
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 128-20, 128, 20)); // bottom

        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 0, 17, 20)); // top-left
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 128-20, 17, 20)); // bottom-left
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(128-17, 0, 17, 20)); // top-right
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(128-17, 128-20, 17, 20)); // bottom-right
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 20, 17, 128-2*20)); // left-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(128-17, 20, 17, 128-2*20)); // right-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(17, 0, 128-2*17, 20)); // top-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(17, 128-20, 128-2*17, 20)); // bottom-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(17, 20, 128-2*17, 128-2*20)); // middle-center

        Vector2 size = texture.GetSize();

        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(0, 0, left, top)); // top-left
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(0, size.Y - bottom, left, bottom)); // bottom-left
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(size.X - right, 0, right, top)); // top-right
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(size.X - right, size.Y - bottom, right, bottom)); // bottom-right
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(0, top, left, size.Y - top - bottom)); // left-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(size.X - right, top, right, size.Y - top - bottom)); // right-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(left, 0, size.X - left - right, top)); // top-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(left, size.Y - bottom, size.X - left - right, bottom)); // bottom-center
        // DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, size), new Rect2(left, top, size.X - left - right, size.Y - top - bottom)); // middle-center

        Rect2 topLeft = new(0, 0, left, top);
        Rect2 bottomLeft = new(0, size.Y - bottom, left, bottom);

        Rect2 topRight = new(size.X - right, 0, right, top);
        Rect2 bottomRight = new(size.X - right, size.Y - bottom, right, bottom);

        Rect2 middleLeft = new(0, top, left, size.Y - top - bottom);
        Rect2 middleRight = new(size.X - right, top, right, size.Y - top - bottom);

        Rect2 topCenter = new(left, 0, size.X - left - right, top);
        Rect2 bottomCenter = new(left, size.Y - bottom, size.X - left - right, bottom);
        Rect2 middleCenter = new(left, top, size.X - left - right, size.Y - top - bottom);

        // DrawRect(new(Vector2.Zero, size), new Color(1, 1, 1));

        // DrawTextureRectRegion(texture, new Rect2(topLeft.Position, topLeft.Size / Scale), topLeft); // top-left
        // DrawTextureRectRegion(texture, new Rect2(bottomLeft.Position.X, bottomLeft.Position.Y + bottomLeft.Size.Y / Scale.Y * 2, bottomLeft.Size / Scale), bottomLeft); // bottom-left
        // DrawTextureRectRegion(texture, new Rect2(topRight.Position.X + topRight.Size.X / 2, topRight.Position.Y, topRight.Size / Scale), topRight); // top-right
        // DrawTextureRectRegion(texture, new Rect2(bottomRight.Position.X + bottomRight.Size.X / 2, bottomRight.Position.Y + bottomRight.Size.Y / Scale.Y * 2, bottomRight.Size / Scale), bottomRight); // bottom-right
        // DrawTextureRectRegion(texture, new Rect2(middleLeft.Position.X, middleLeft.Position.Y, middleLeft.Size.X / Scale.X, middleLeft.Size.Y), middleLeft); // left-center
        // DrawTextureRectRegion(texture, new Rect2(middleRight.Position.X + middleRight.Size.X / 2, middleRight.Position.Y, middleRight.Size.X / Scale.X, middleRight.Size.Y), middleRight); // right-center
        // DrawTextureRectRegion(texture, new Rect2(topCenter.Position, topCenter.Size.X, topCenter.Size.Y / Scale.Y), topCenter); // top-center
        // DrawTextureRectRegion(texture, new Rect2(bottomCenter.Position.X, bottomCenter.Position.Y + bottomCenter.Size.Y / Scale.Y * 2, bottomCenter.Size.X, bottomCenter.Size.Y / Scale.Y), bottomCenter); // bottom-center
        // DrawTextureRectRegion(texture, middleCenter, middleCenter); // middle-center

        // DrawTextureRectRegion(texture, new Rect2(topLeft.Position, topLeft.Size / Scale), topLeft); // top-left
        // DrawTextureRectRegion(texture, new Rect2(bottomLeft.Position.X, bottomLeft.Position.Y + bottomLeft.Size.Y / Scale.Y * 2, bottomLeft.Size / Scale), bottomLeft); // bottom-left
        // DrawTextureRectRegion(texture, new Rect2(topRight.Position.X + topRight.Size.X / 2, topRight.Position.Y, topRight.Size / Scale), topRight); // top-right
        // DrawTextureRectRegion(texture, new Rect2(bottomRight.Position.X + bottomRight.Size.X / 2, bottomRight.Position.Y + bottomRight.Size.Y / Scale.Y * 2, bottomRight.Size / Scale), bottomRight); // bottom-right
        // DrawTextureRectRegion(texture, new Rect2(middleLeft.Position.X, middleLeft.Position.Y - topLeft.Size.Y / Scale.Y * 2, middleLeft.Size.X / Scale.X, middleLeft.Size.Y + topLeft.Size.Y * Scale.Y / 2), middleLeft); // left-center
        // DrawTextureRectRegion(texture, new Rect2(middleRight.Position.X + middleRight.Size.X / 2, middleRight.Position.Y - topRight.Size.Y / Scale.Y * 2, middleRight.Size.X / Scale.X, middleRight.Size.Y + topRight.Size.Y * Scale.Y / 2), middleRight); // right-center
        // DrawTextureRectRegion(texture, new Rect2(topCenter.Position.X - left / 2, topCenter.Position.Y, topCenter.Size.X + (left + right) / 2, topCenter.Size.Y / Scale.Y), topCenter); // top-center
        // DrawTextureRectRegion(texture, new Rect2(bottomCenter.Position.X - left / 2, bottomCenter.Position.Y + bottomCenter.Size.Y / Scale.Y * 2, bottomCenter.Size.X + (left + right) / 2, bottomCenter.Size.Y / Scale.Y), bottomCenter); // bottom-center
        // DrawTextureRectRegion(texture, middleCenter, middleCenter); // middle-center

        // FIXME: as much as I don't want to touch this eldritch mess, the math is wrong
        DrawTextureRectRegion(texture, new Rect2(topLeft.Position, topLeft.Size / Scale), topLeft); // top-left
        DrawTextureRectRegion(texture, new Rect2(bottomLeft.Position.X, bottomLeft.Position.Y + bottomLeft.Size.Y / Scale.Y * 2, bottomLeft.Size / Scale), bottomLeft); // bottom-left
        DrawTextureRectRegion(texture, new Rect2(topRight.Position.X + topRight.Size.X / 2, topRight.Position.Y, topRight.Size / Scale), topRight); // top-right
        DrawTextureRectRegion(texture, new Rect2(bottomRight.Position.X + bottomRight.Size.X / 2, bottomRight.Position.Y + bottomRight.Size.Y / Scale.Y * 2, bottomRight.Size / Scale), bottomRight); // bottom-right
        DrawTextureRectRegion(texture, new Rect2(middleLeft.Position.X, middleLeft.Position.Y - topLeft.Size.Y / Scale.Y * 2, middleLeft.Size.X / Scale.X, middleLeft.Size.Y + topLeft.Size.Y * Scale.Y / 2), middleLeft); // left-center
        DrawTextureRectRegion(texture, new Rect2(middleRight.Position.X + middleRight.Size.X / 2, middleRight.Position.Y - topRight.Size.Y / Scale.Y * 2, middleRight.Size.X / Scale.X, middleRight.Size.Y + topRight.Size.Y * Scale.Y / 2), middleRight); // right-center
        DrawTextureRectRegion(texture, new Rect2(topCenter.Position.X - left / 2, topCenter.Position.Y, topCenter.Size.X + (left + right) / 2, topCenter.Size.Y / Scale.Y), topCenter); // top-center
        DrawTextureRectRegion(texture, new Rect2(bottomCenter.Position.X - left / 2, bottomCenter.Position.Y + bottomCenter.Size.Y / Scale.Y * 2, bottomCenter.Size.X + (left + right) / 2, bottomCenter.Size.Y / Scale.Y), bottomCenter); // bottom-center
        DrawTextureRectRegion(texture, new Rect2(middleCenter.Position.X - left / 2, middleCenter.Position.Y - topLeft.Size.Y / Scale.Y * 2, middleCenter.Size.X + (left + right) / 2, middleCenter.Size.Y + (top + bottom) / Scale.Y * 2), middleCenter); // middle-center
    }

    // private void DrawHold() {
    //     DrawTextureRectRegion(texture,new Rect2(Vector2.Zero, texture.GetSize()), new Rect2(0, 20, 128, 0));
    // }
}