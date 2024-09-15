using System;
using Godot;

namespace PCE.Editor;

[GlobalClass, Obsolete("Use OS.Alert instead")]
public sealed partial class ErrorDialog : AcceptDialog
{
    public static ErrorDialog Singleton => ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorDialog>("PCE_ErrorDialog");

    private Label content;

    public sealed override void _Ready()
    {
        InitialPosition = WindowInitialPosition.CenterMainWindowScreen;
        PopupWindow = true;
        ForceNative = true;

        content = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AnchorBottom = 1,
            AnchorLeft = 0,
            AnchorRight = 1,
            AnchorTop = 0,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Begin,
        };
        AddChild(content);
    }

    public void Show(string title, string message)
    {
        Title = title;
        content.Text = message;
        Show();
    }
}