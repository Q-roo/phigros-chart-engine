/**************************************************************************/
/*  editor_spin_slider.h, editor_spin_slider.cpp                          */
/**************************************************************************/
/*                         This file is part of:                          */
/*                             GODOT ENGINE                               */
/*                        https://godotengine.org                         */
/**************************************************************************/
/* Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md). */
/* Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.                  */
/*                                                                        */
/* Permission is hereby granted, free of charge, to any person obtaining  */
/* a copy of this software and associated documentation files (the        */
/* "Software"), to deal in the Software without restriction, including    */
/* without limitation the rights to use, copy, modify, merge, publish,    */
/* distribute, sublicense, and/or sell copies of the Software, and to     */
/* permit persons to whom the Software is furnished to do so, subject to  */
/* the following conditions:                                              */
/*                                                                        */
/* The above copyright notice and this permission notice shall be         */
/* included in all copies or substantial portions of the Software.        */
/*                                                                        */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,        */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF     */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. */
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY   */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,   */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE      */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
/**************************************************************************/

using System;
using Godot;
using Godot.Collections;
using Range = Godot.Range;

namespace PCE.Editor;

[GlobalClass, Tool]
public partial class SpinSlider : Range {
    [Signal]
    public delegate void GrabbedEventHandler();
    [Signal]
    public delegate void UngrabbedEventHandler();
    [Signal]
    public delegate void ValueFocusEnteredEventHandler();
    [Signal]
    public delegate void ValueFocusExitedEventHandler();

    const float EDSCALE = 1;

    private struct ThemeCache {
        public Texture2D updownIcon;
        public Texture2D updownDisabledIcon;

        public ThemeCache Init() {
            Resize(ref updownIcon);
            Resize(ref updownDisabledIcon);
            return this;
        }

        private static void Resize(ref Texture2D texture2D) {
            Image img = texture2D.GetImage();
            img.Resize(30, 30);
            ImageTexture itex = new();
            itex.SetImage(img);
            texture2D = itex;
        }
    }

    private string _label;
    [Export]
    public string Label { get => _label; set { _label = value; QueueRedraw(); } }
    private string _suffix;
    [Export]
    public string Suffix { get => _suffix; set { _suffix = value; QueueRedraw(); } }
    private int updownOffset = -1;
    private bool hoverUpdown = false;
    private bool mouseHover = false;

    private TextureRect grabber;
    private int grabberRange = 1;

    private bool mouseOverSpin = false;
    private bool mouseOverGrabber = false;
    private bool mousewheelOverGrabber = false;

    private bool grabbingGrabber = false;
    private int grabbingFrom = 0;
    private float grabbingRatio = 0.0f;

    private bool grabbingSpinnerAttempt = false;
    private bool grabbingSpinner = false;

    private bool _readOnly = false;
    [Export]
    public bool ReadOnly {
        get => _readOnly; set {
            _readOnly = value;
            if (_readOnly && LineEdit is not null && LineEdit.IsInsideTree())
                LineEdit.ReleaseFocus();
        }
    }
    private float grabbingSpinnerDistCache = 0.0f;
    private float grabbingSpinnerSpeed = 0.0f;
    private Vector2 grabbingSpinnerMousePos;
    private double preGrabValue = 0.0;

    private Control valueInputPopup;
    public LineEdit LineEdit { get; private set; }
    private ulong valueInputClosedFrame = 0;
    private bool valueInputDirty = false;

    private ThemeCache themeCache = (new ThemeCache() {
        updownIcon = ResourceLoader.Load<Texture2D>("res://icon.svg"),
        updownDisabledIcon = ResourceLoader.Load<Texture2D>("res://icon.svg"),
    }).Init();

    private bool _hiseSlider = false;
    [Export]
    public bool HideSlider { get => _hiseSlider; set { _hiseSlider = value; QueueRedraw(); } }
    private bool _flat = false;
    [Export]
    public bool Flat { get => _flat; set { _flat = value; QueueRedraw(); } }

    public bool Grabbing => grabbingGrabber || grabbingSpinner;
    public string TextValue => Value.ToString($"0.{Mathf.StepDecimals(Step)}f");

    public SpinSlider() {
        FocusMode = FocusModeEnum.All;
        grabber = new();
        AddChild(grabber);
        grabber.Hide();
        grabber.TopLevel = true;
        grabber.MouseFilter = MouseFilterEnum.Stop;
        grabber.MouseEntered += GrabberMouseEntered;
        grabber.MouseExited += GrabberMouseExited;
        grabber.GuiInput += GrabberGuiInput;
    }

    public override void _Notification(int what) {
        switch ((long)what) {
            case NotificationEnterTree: {
                grabbingSpinnerSpeed = 1;//EditorSettings::get_singleton()->get("interface/inspector/float_drag_speed");
                EnsureInputPopup();
                UpdateValueInputStylebox();
            }
            break;

            case NotificationThemeChanged: {
                EnsureInputPopup();
                UpdateValueInputStylebox();
            }
            break;

            case NotificationInternalProcess: {
                if (valueInputDirty) {
                    valueInputDirty = false;
                    LineEdit.Text = TextValue;
                }

                SetProcessInternal(false);
            }
            break;

            case NotificationDraw: {
                DrawSpinSlider();
            }
            break;

            case NotificationWMWindowFocusIn:
            case NotificationWMWindowFocusOut:
            case NotificationWMCloseRequest:
            case NotificationExitTree: {
                if (grabbingSpinner) {
                    grabber.Hide();
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    Input.WarpMouse(grabbingSpinnerMousePos);
                    grabbingSpinner = false;
                    grabbingSpinnerAttempt = false;
                }
            }
            break;

            case NotificationMouseEnter: {
                mouseOverSpin = true;
                QueueRedraw();
            }
            break;

            case NotificationMouseExit: {
                mouseOverSpin = false;
                QueueRedraw();
            }
            break;

            case NotificationFocusEnter: {
                if ((Input.IsActionPressed("ui_focus_next") || Input.IsActionPressed("ui_focus_prev")) && valueInputClosedFrame != (ulong)Engine.GetFramesDrawn()) {
                    _FocusEntered();
                }
                valueInputClosedFrame = 0;
            }
            break;
        }
    }

    public override void _GuiInput(InputEvent @event) {
        if (ReadOnly)
            return;

        if (@event is InputEventMouseButton mb) {
            if (mb.ButtonIndex == MouseButton.Left) {
                if (mb.Pressed) {
                    if (updownOffset != -1 && ((!IsLayoutRtl() && mb.Position.X > updownOffset) || (IsLayoutRtl() && mb.Position.X < updownOffset))) {
                        // Updown pressed.
                        if (mb.Position.Y < Size.Y / 2f) {
                            Value += Step;
                        } else {
                            Value -= Step;
                        }
                        return;
                    }
                    GrabStart();
                } else {
                    GrabEnd();
                }
            } else if (mb.ButtonIndex == MouseButton.Right) {
                if (mb.Pressed && Grabbing) {
                    GrabEnd();
                    Value = preGrabValue;
                }
            } else if (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown) {
                if (grabber.Visible) {
                    new Callable((CanvasItem)this, MethodName.QueueRedraw).CallDeferred();
                }
            }
        }

        if (@event is InputEventMouseMotion mm) {
            if (grabbingSpinnerAttempt) {
                double diffX = mm.Relative.X;
                if (mm.ShiftPressed && grabbingSpinner) {
                    diffX *= 0.1;
                }
                grabbingSpinnerDistCache += (float)(diffX * grabbingSpinnerSpeed);

                if (!grabbingSpinner && Mathf.Abs(grabbingSpinnerDistCache) > 4 * grabbingSpinnerSpeed * EDSCALE) {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                    grabbingSpinner = true;
                }

                if (grabbingSpinner) {
                    // Don't make the user scroll all the way back to 'in range' if they went off the end.
                    if (preGrabValue < MinValue && !AllowLesser) {
                        preGrabValue = MinValue;
                    }
                    if (preGrabValue > MaxValue && !AllowGreater) {
                        preGrabValue = MaxValue;
                    }

                    if (mm.IsCommandOrControlPressed()) {
                        // If control was just pressed, don't make the value do a huge jump in magnitude.
                        if (grabbingSpinnerDistCache != 0) {
                            preGrabValue += grabbingSpinnerDistCache * Step;
                            grabbingSpinnerDistCache = 0;
                        }

                        Value = Mathf.Round(preGrabValue + Step * grabbingSpinnerDistCache * 10);
                    } else {
                        Value = preGrabValue + Step * grabbingSpinnerDistCache;
                    }
                }
            } else if (updownOffset != -1) {
                bool newHover = (!IsLayoutRtl() && mm.Position.X > updownOffset) || (IsLayoutRtl() && mm.Position.X < updownOffset);
                if (newHover != hoverUpdown) {
                    hoverUpdown = newHover;
                    QueueRedraw();
                }
            }
        }

        if (@event is InputEventKey k) {
            if (k.IsAction("ui_accept", true)) {
                _FocusEntered();
            } else if (Grabbing) {
                if (k.IsAction("ui_cancel", true)) {
                    GrabEnd();
                    Value = preGrabValue;
                }
                AcceptEvent();
            }
        }
    }

    private void GrabStart() {
        grabbingSpinnerAttempt = true;
        grabbingSpinnerDistCache = 0;
        preGrabValue = Value;
        grabbingSpinner = false;
        grabbingSpinnerMousePos = GetGlobalMousePosition();
        EmitSignal(SignalName.Grabbed);
    }
    private void GrabEnd() {
        if (grabbingSpinnerAttempt) {
            if (grabbingSpinner) {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                Input.WarpMouse(grabbingSpinnerMousePos);
                QueueRedraw();
                grabbingSpinner = false;
                EmitSignal(SignalName.Ungrabbed);
            } else {
                _FocusEntered();
            }

            grabbingSpinnerAttempt = false;
        }

        if (grabbingGrabber) {
            grabbingGrabber = false;
            mousewheelOverGrabber = false;
            EmitSignal(SignalName.Ungrabbed);
        }
    }

    private void GrabberGuiInput(InputEvent @event) {
        if (ReadOnly)
            return;

        if (@event is InputEventMouseButton mb) {
            if (grabbingGrabber) {
                if (mb.ButtonIndex == MouseButton.WheelUp) {
                    Value += Step;
                    mousewheelOverGrabber = true;
                } else if (mb.ButtonIndex == MouseButton.WheelDown) {
                    Value -= Step;
                    mousewheelOverGrabber = true;
                }
            }

            if (mb.ButtonIndex == MouseButton.Right) {
                if (mb.Pressed) {
                    grabbingGrabber = true;
                    preGrabValue = Value;
                    if (!mousewheelOverGrabber) {
                        grabbingRatio = (float)Ratio;
                        grabbingFrom = (int)(grabber.GetTransform() * mb.Position).X;
                    }
                    GrabFocus();
                    EmitSignal(SignalName.Grabbed);
                } else {
                    grabbingGrabber = false;
                    mousewheelOverGrabber = false;
                    EmitSignal(SignalName.Ungrabbed);
                }
            } else if (mb.ButtonIndex == MouseButton.Right) {
                if (mb.Pressed && grabbingGrabber) {
                    grabbingGrabber = false;
                    mousewheelOverGrabber = false;
                    Value = preGrabValue;
                    EmitSignal(SignalName.Ungrabbed);
                }
            }
        }

        if (@event is InputEventMouseMotion mm && grabbingGrabber) {
            if (mousewheelOverGrabber) {
                return;
            }

            float scaleX = GetGlobalTransformWithCanvas().Scale.X;
            if (Mathf.IsZeroApprox(scaleX))
                throw new Exception("Mathf.IsZeroApprox(scaleX) is true");
            float grabbingOfs = ((grabber.GetTransform() * mm.Position).X - grabbingFrom) / grabberRange / scaleX;
            Ratio = grabbingRatio + grabbingOfs;
            QueueRedraw();
        }
    }
    //modal_closed signal
    private void ValueInputClosed() {
        EvaluateInputText();
        valueInputClosedFrame = (ulong)Engine.GetFramesDrawn();
    }
    //text_submitted signal
    private void ValueInputSubmitted(string text) {
        valueInputClosedFrame = (ulong)Engine.GetFramesDrawn();
        valueInputPopup?.Hide();
    }
    //focus_exited signal
    private void _ValueFocusExited() {
        // discontinue because the focus_exit was caused by right-click context menu
        if (LineEdit.IsMenuVisible()) {
            return;
        }

        if (ReadOnly) {
            // Spin slider has become read only while it was being edited.
            return;
        }

        EvaluateInputText();
        // focus is not on the same element after the value_input was exited
        // -> focus is on next element
        // -> TAB was pressed
        // -> modal_close was not called
        // -> need to close/hide manually
        if (!IsVisibleInTree() || valueInputClosedFrame != (ulong)Engine.GetFramesDrawn()) {
            // Hidden or something else took focus.
            valueInputPopup?.Hide();
        } else {
            // Enter or Esc was pressed.
            GrabFocus();
        }

        EmitSignal(SignalName.ValueFocusExited);
    }
    private void ValueInputGuiInput(InputEvent @event) {
        if (@event is InputEventKey k && k.Pressed && !ReadOnly) {
            Key code = k.Keycode;

            switch (code) {
                case Key.Up:
                case Key.Down: {
                    double step = Step;
                    if (step < 1) {
                        double divisor = 1.0 / step;

                        if (Math.Truncate(divisor) == divisor) {
                            step = 1.0;
                        }
                    }

                    if (k.IsCommandOrControlPressed()) {
                        step *= 100.0;
                    } else if (k.ShiftPressed) {
                        step *= 10.0;
                    } else if (k.AltPressed) {
                        step *= 0.1;
                    }

                    EvaluateInputText();

                    double lastValue = Value;
                    if (code == Key.Down) {
                        step *= -1;
                    }
                    Value = lastValue + step;

                    valueInputDirty = true;
                    SetProcessInternal(true);
                }
                break;
                case Key.Escape: {
                    valueInputClosedFrame = (ulong)Engine.GetFramesDrawn();
                    valueInputPopup?.Hide();
                }
                break;
                default:
                    break;
            }
        }
    }

    public override string _GetTooltip(Vector2 atPosition) {
        if (!ReadOnly && grabber.Visible) {
            Key key = (OS.HasFeature("macos") || OS.HasFeature("web_macos") || OS.HasFeature("web_ios")) ? Key.Meta : Key.Ctrl;
            return Value.ToString() + "\n\n" + Tr($"Hold {key} to round to integers.\nHold Shift for more precise changes.");
        }
        return Value.ToString();
    }

    public override Vector2 _GetMinimumSize() {
        StyleBox sb = GetThemeStylebox("normal", "LineEdit");
        Font font = GetThemeFont("font", "LineEdit");
        int fontSize = GetThemeFontSize("font_size", "LineEdit");

        Vector2 ms = sb.GetMinimumSize();
        ms.Y += font.GetHeight(fontSize);

        return ms;
    }

    private void EvaluateInputText() {
        var TS = TextServerManager.GetPrimaryInterface();
        Expression expr = new();

        // Convert commas ',' to dots '.' for French/German etc. keyboard layouts.
        string text = LineEdit.Text.Replace(",", ".");
        text = text.Replace(";", ",");
        text = TS.ParseNumber(text);

        Error err = expr.Parse(text);
        if (err != Error.Ok) {
            // If the expression failed try without converting commas to dots - they might have been for parameter separation.
            text = LineEdit.Text;
            text = TS.ParseNumber(text);

            err = expr.Parse(text);
            if (err != Error.Ok) {
                return;
            }
        }

        Variant v = expr.Execute([], null, false, true);
        if (v.VariantType == Variant.Type.Nil) {
            return;
        }
        Value = v.As<double>();
    }

    private void UpdateValueInputStylebox() {
        // Add a left margin to the stylebox to make the number align with the Label
        // when it's edited. The LineEdit "focus" stylebox uses the "normal" stylebox's
        // default margins.
        StyleBox stylebox = (StyleBox)GetThemeStylebox("normal", "LineEdit").Duplicate();
        // EditorSpinSliders with a label have more space on the left, so add an
        // higher margin to match the location where the text begins.
        // The margin values below were determined by empirical testing.
        if (IsLayoutRtl()) {
            stylebox.ContentMarginRight = (!string.IsNullOrEmpty(Label) ? 23 : 16) * EDSCALE;
        } else {
            stylebox.ContentMarginLeft = (!string.IsNullOrEmpty(Label) ? 23 : 16) * EDSCALE;
        }

        LineEdit.AddThemeStyleboxOverride("normal", stylebox);
    }
    private void EnsureInputPopup() {
        if (valueInputPopup is not null)
            return;

        valueInputPopup = new();
        valueInputPopup.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(valueInputPopup);

        LineEdit = new() {
            FocusMode = FocusModeEnum.Click
        };
        valueInputPopup.AddChild(LineEdit);
        LineEdit.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        valueInputPopup.Hidden += ValueInputClosed;
        LineEdit.TextSubmitted += ValueInputSubmitted;
        LineEdit.FocusExited += _ValueFocusExited;
        LineEdit.GuiInput += ValueInputGuiInput;

        if (IsInsideTree())
            UpdateValueInputStylebox();
    }
    private void DrawSpinSlider() {
        updownOffset = -1;

        Rid ci = GetCanvasItem();
        bool rtl = IsLayoutRtl();
        Vector2 size = Size;

        StyleBox sb = GetThemeStylebox(ReadOnly ? "ReadOnly" : "normal", "LineEdit");
        if (!Flat) {
            DrawStyleBox(sb, new(new(), size));
        }

        Font font = GetThemeFont("font", "LineEdit");
        int fontSize = GetThemeFontSize("font_size", "LineEdit");
        int sepBase = (int)(4 * EDSCALE);
        int sep = sepBase + (int)sb.GetOffset().X; //make it have the same margin on both sides, looks better

        int labelWidth = (int)font.GetStringSize(Label, HorizontalAlignment.Left, -1, fontSize).X;
        int numberWidth = (int)(size.X - sb.GetMinimumSize().X - labelWidth - sep);

        Texture2D updown = GetThemeIcon(ReadOnly ? "updown_disabled" : "updown", "SpinBox");

        string numstr = TextValue;

        int vofs = (int)((size.Y - font.GetHeight(fontSize)) / 2f + font.GetAscent(fontSize));

        Color fc = GetThemeColor(ReadOnly ? "font_uneditable_color" : "font_color", "LineEdit");
        Color lc = GetThemeColor(ReadOnly ? "ReadOnly_label_color" : "label_color");

        if (Flat && !string.IsNullOrEmpty(Label)) {
            StyleBox labelBg = GetThemeStylebox("label_bg", "EditorSpinSlider");
            if (rtl) {
                DrawStyleBox(labelBg, new(new(size.X - (sb.GetOffset().X * 2 + labelWidth), 0), new(sb.GetOffset().X * 2 + labelWidth, size.Y)));
            } else {
                DrawStyleBox(labelBg, new(new(), new(sb.GetOffset().X * 2 + labelWidth, size.Y)));
            }
        }

        if (HasFocus()) {
            StyleBox focus = GetThemeStylebox("focus", "LineEdit");
            DrawStyleBox(focus, new(new(), size));
        }

        if (rtl) {
            DrawString(font, new Vector2(Mathf.Round(size.X - sb.GetOffset().X - labelWidth), vofs), Label, HorizontalAlignment.Right, -1, fontSize, lc * new Color(1, 1, 1, 0.5f));
        } else {
            DrawString(font, new Vector2(Mathf.Round(sb.GetOffset().X), vofs), Label, HorizontalAlignment.Left, -1, fontSize, lc * new Color(1, 1, 1, 0.5f));
        }

        var TS = TextServerManager.GetPrimaryInterface();

        int suffixStart = numstr.Length;
        Rid numRid = TS.CreateShapedText();
        TS.ShapedTextAddString(numRid, numstr + "\u2009" + Suffix, font.GetRids(), fontSize, font.GetOpentypeFeatures());

        float textStart = rtl ? Mathf.Round(sb.GetOffset().X) : Mathf.Round(sb.GetOffset().X + labelWidth + sep);
        Vector2 textOfs = rtl ? new(textStart + (numberWidth - (float)TS.ShapedTextGetWidth(numRid)), vofs) : new(textStart, vofs);
        int vSize = (int)TS.ShapedTextGetGlyphCount(numRid);
        Array<Dictionary> glyphs = TS.ShapedTextGetGlyphs(numRid);
        // start: int
        // end: int
        // repeat: uint8_t
        // count: uint8_t
        // flags: uint16_t
        // offset: Vector2
        // advance: float
        // font_rid: RID
        // font_size: int
        // index: int32_t
        for (int i = 0; i < vSize; i++) {
            byte gRepeat = glyphs[i]["repeat"].As<byte>();
            float gAdvance = glyphs[i]["advance"].As<float>();
            int gStart = glyphs[i]["start"].As<int>();
            Rid gRid = glyphs[i]["font_rid"].As<Rid>();
            int gFontSize = glyphs[i]["font_size"].As<int>();
            Vector2 gOffset = glyphs[i]["offset"].As<Vector2>();
            int gIndex = glyphs[i]["index"].As<int>();
            ushort gFlags = glyphs[i]["flags"].As<ushort>();

            for (int j = 0; j < gRepeat; j++) {
                if (textOfs.X >= textStart && (textOfs.X + gAdvance) <= (textStart + numberWidth)) {
                    Color color = fc;
                    if (gStart >= suffixStart) {
                        color.A *= 0.4f;
                    }
                    if (gRid != new Rid()) {
                        TS.FontDrawGlyph(gRid, ci, gFontSize, textOfs + gOffset, gIndex, color);
                    } else if ((gFlags & (int)TextServer.GraphemeFlag.Virtual) != (int)TextServer.GraphemeFlag.Virtual) {
                        TS.DrawHexCodeBox(ci, gFontSize, textOfs + gOffset, gIndex, color);
                    }
                }
                textOfs.X += gAdvance;
            }
        }
        TS.FreeRid(numRid);

        if (!HideSlider) {
            if (Step == 1) {
                Texture2D updown2 = ReadOnly ? themeCache.updownDisabledIcon : themeCache.updownIcon;
                int updownVofs = (int)(size.Y - updown2.GetHeight()) / 2;
                if (rtl) {
                    updownOffset = (int)sb.GetMargin(Side.Left);
                } else {
                    updownOffset = (int)(size.X - sb.GetMargin(Side.Right) - updown2.GetWidth());
                }
                Color c = new(1, 1, 1);
                if (hoverUpdown) {
                    c *= new Color(1.2f, 1.2f, 1.2f);
                }
                DrawTexture(updown2, new(updownOffset, updownVofs), c);
                if (rtl) {
                    updownOffset += updown2.GetWidth();
                }
                if (grabber.Visible) {
                    grabber.Hide();
                }
            } else {
                int grabberW = (int)(4 * EDSCALE);
                int width = (int)(size.X - sb.GetMinimumSize().X - grabberW);
                int ofs = (int)sb.GetOffset().X;
                int svofs = (int)(size.Y + vofs) / 2 - 1;
                Color c = fc;

                // Draw the horizontal slider's background.
                c.A = 0.2f;
                DrawRect(new(ofs, svofs + 1, width, 2 * EDSCALE), c);

                // Draw the horizontal slider's filled part on the left.
                int gofs = (int)(Ratio * width);
                c.A = 0.45f;
                DrawRect(new(ofs, svofs + 1, gofs, 2 * EDSCALE), c);

                // Draw the horizontal slider's grabber.
                c.A = 0.9f;
                Rect2 grabberRect = new(ofs + gofs, svofs, grabberW, 4 * EDSCALE);
                DrawRect(grabberRect, c);

                grabbingSpinnerMousePos = GlobalPosition + grabberRect.GetCenter();

                bool displayGrabber = !ReadOnly && (grabbingGrabber || mouseOverSpin || mouseOverGrabber) && !grabbingSpinner && !(valueInputPopup is not null && valueInputPopup.Visible);
                if (grabber.Visible != displayGrabber) {
                    grabber.Visible = displayGrabber;
                }

                if (displayGrabber) {
                    Texture2D grabberTex;
                    if (mouseOverGrabber) {
                        grabberTex = GetThemeIcon("grabber_highlight", "HSlider");
                    } else {
                        grabberTex = GetThemeIcon("grabber", "HSlider");
                    }

                    if (grabber.Texture != grabberTex) {
                        grabber.Texture = grabberTex;
                    }

                    Vector2 scale = GetGlobalTransformWithCanvas().Scale;
                    grabber.Scale = scale;
                    grabber.ResetSize();
                    grabber.Position = GlobalPosition + (grabberRect.GetCenter() - grabber.Size * 0.5f) * scale;

                    if (mousewheelOverGrabber) {
                        Input.WarpMouse(grabber.Position + grabberRect.Size);
                    }

                    grabberRange = width;
                }
            }
        }
    }

    protected void GrabberMouseEntered() {
        mouseHover = true;
        QueueRedraw();
    }
    protected void GrabberMouseExited() {
        mouseHover = false;
        QueueRedraw();
    }
    protected void _FocusEntered() {
        if (ReadOnly)
            return;

        EnsureInputPopup();
        LineEdit.Text = TextValue;
        valueInputPopup.Size = Size;
        LineEdit.FocusNext = FindNextValidFocus().GetPath();
        LineEdit.FocusPrevious = FindPrevValidFocus().GetPath();
        new Callable((CanvasItem)valueInputPopup, CanvasItem.MethodName.Show).CallDeferred();
        new Callable((Control)LineEdit, Control.MethodName.GrabFocus).CallDeferred();
        new Callable(LineEdit, LineEdit.MethodName.SelectAll).CallDeferred();
        EmitSignal(SignalName.ValueFocusEntered);
    }

    public void SetupAndShow() { _FocusEntered(); }
}