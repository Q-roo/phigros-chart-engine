using Godot;

namespace PCE.Chartbuild.Runtime;

public class Unset() : Object(null) {
    public override bool ToBool() => false;
    public override float ToF32() => 0f;
    public override int ToI32() => 0;
    public override Vector2 ToVec2() => Vector2.Zero;
    public override string ToString() => "unset";

    public override Object Copy(bool shallow = true, params object[] keys) => this;
}