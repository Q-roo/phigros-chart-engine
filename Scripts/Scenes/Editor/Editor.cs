using Godot;
using System.Globalization;

namespace PCE.Editor;

public partial class Editor : Control
{
    public override void _EnterTree()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    }
}
