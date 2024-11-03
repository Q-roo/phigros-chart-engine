/**************************************************************************/
/*  animation_track_editor.h, animation_track_editor.cpp                  */
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

using Range = Godot.Range;

namespace PCE.Editor;

// https://github.com/godotengine/godot/blob/master/editor/animation_track_editor.cpp#L1288
// https://github.com/godotengine/godot/blob/master/editor/animation_track_editor.h#L186
// https://github.com/godotengine/godot/blob/master/editor/animation_bezier_editor.cpp

[GlobalClass]
public partial class AnimationTrackTimelineEdit : Range {
    public const float ScrollZoomFactorIn = 1.02f; // Zoom factor per mouse scroll in the animation editor when zooming in. The closer to 1.0, the finer the control.
    public const float ScrollZoomFactorOut = 0.98f; // Zoom factor when zooming out. Similar to SCROLL_ZOOM_FACTOR_IN but less than 1.0.
    public const double FpsDecimal = 1.0;
    public const double SecondDecimal = 0.0001;
    public const double FpsStepFraction = 0.0625;
    public const float EDSCALE = 1f;
    public const int ScAdj = 100;

    [Signal]
    public delegate void ZoomChangedEventHandler();
    [Signal]
    public delegate void LengthChangedEventHandler(double newLen);
    [Signal]
    public delegate void TimelineChangedEventHandler(float ofc, bool altPressed);
    [Signal]
    public delegate void NameLimitChangedEventHandler();
    [Signal]
    public delegate void TrackAddedEventHandler(int trackIndex);

    private Rect2 hsizeRect;

    private bool editing = false;

    private ViewPanner panner;

    private bool draggingTimeline = false;
    private bool draggingHsize = false;
    private float draggingHsizeFrom = 0.0f;
    private float draggingHsizeAt = 0.0f;
    private double lastZoomScale = 1.0;
    private double hscrollOnZoomBuffer = -1.0;

    private Vector2 zoomScrollOrigin;
    private bool zoomCallbackOccured = false;

    private Animation animation;
    private bool readOnly = false;

    // AnimationTrackEdit *track_edit;
    private int nameLimit = 0;
    private Range hScroll;
    private float playPositionPos = 0.0f;

    private HBoxContainer lenHb;
    private SpinSlider length;
    private Button loop;
    private TextureRect timeIcon;

    private MenuButton addTrack;
    private Control playPosition; //separate control used to draw so updates for only position changed are much faster
    private HScrollBar hscroll;

    public float ZoomScale => GetZoomScale(_zoom.Value);
    public int ButtonsWidth {
        get {
            Texture2D interpMode = GetThemeIcon("TrackContinuous");
            Texture2D interpType = GetThemeIcon("InterpRaw");
            Texture2D loopType = GetThemeIcon("InterpWrapClamp");
            Texture2D removeIcon = GetThemeIcon("Remove");
            Texture2D downIcon = GetThemeIcon("select_arrow", "Tree");

            int hSeparation = GetThemeConstant("h_separation", "AnimationTrackEdit");

            int totalW = interpMode.GetWidth() + interpType.GetWidth() + loopType.GetWidth() + removeIcon.GetWidth();
            totalW += (downIcon.GetWidth() + hSeparation) * 4;

            return totalW;
        }
    }
    public int NameLimit {
        get {
            Texture2D hsizeIcon = GetThemeIcon("Hsize");

            int limit = (int)Mathf.Max(nameLimit, addTrack.GetMinimumSize().X + hsizeIcon.GetWidth() + 8 * EDSCALE);

            limit = (int)Mathf.Min(limit, Size.X - ButtonsWidth - 1);

            return limit;
        }
    }
    private Range _zoom;
    public Range Zoom {
        get => _zoom;
        set {
            _zoom = value;
            _zoom.ValueChanged += _ZoomChanged;
        }
    }
    public float PlayPosition {
        get => playPositionPos;
        set {
            playPositionPos = value;
            QueueRedraw();
        }
    }
    private bool _useFps = false;
    public bool UseFps {
        get => _useFps;
        set {
            _useFps = value;
            QueueRedraw();
        }
    }

    public AnimationTrackTimelineEdit() {
        nameLimit = (int)(150 * EDSCALE);

        playPosition = new() {
            MouseFilter = MouseFilterEnum.Pass
        };
        AddChild(playPosition);
        playPosition.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        playPosition.Draw += PlayPositionDraw;

        addTrack = new() {
            Position = Vector2.Zero,
            Text = Tr("Add Track")
        };
        AddChild(addTrack);

        lenHb = new();

        Control expander = new() {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        lenHb.AddChild(expander);
        timeIcon = new() {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = Tr("Animation length (seconds)")
        };
        lenHb.AddChild(timeIcon);
        length = new() {
            MinValue = SecondDecimal,
            MaxValue = 36000,
            Step = SecondDecimal,
            AllowGreater = true,
            CustomMinimumSize = new(70 * EDSCALE, 0),
            HideSlider = true,
            TooltipText = Tr("Animation length (seconds)")
        };
        length.ValueChanged += _AnimNameChanged;
        lenHb.AddChild(length);
        loop = new() {
            Flat = true,
            TooltipText = Tr("Animation Looping")
        };
        loop.Pressed += _AnimLoopPressed;
        loop.ToggleMode = true;
        lenHb.AddChild(loop);
        AddChild(lenHb);

        addTrack.Hide();
        addTrack.GetPopup().IndexPressed += _TrackAdded;
        lenHb.Hide();

        panner = new();
        panner.SetScrollZoomFactor(ScrollZoomFactorIn);
        panner.SetCallbacks(PanCallback, ZoomCallback);
        panner.PanAxis = ViewPanner.PanAxisEnum.Horizontal;

        LayoutDirection = LayoutDirectionEnum.Ltr;
    }

    public override Vector2 _GetMinimumSize() {
        Vector2 ms = addTrack.GetMinimumSize();
        Font font = GetThemeFont("font", "Label");
        int fontSize = GetThemeFontSize("font_size", "Label");
        ms.Y = Mathf.Max(ms.Y, font.GetHeight(fontSize));
        ms.X = ButtonsWidth + addTrack.GetMinimumSize().X + GetThemeIcon("Hsize").GetWidth() + 2 + 8 * EDSCALE;
        return ms;
    }

    public CursorShape GetCursorShape(Vector2 pos) {
        if (draggingHsize || hsizeRect.HasPoint(pos)) {
            // Indicate that the track name column's width can be adjusted
            return CursorShape.Hsize;
        } else {
            return MouseDefaultCursorShape;
        }
    }


    public void SetAnimation(Animation animation, bool readOnly) {
        this.animation = animation;
        this.readOnly = readOnly;

        length.ReadOnly = this.readOnly;

        if (this.animation is not null) {
            lenHb.Show();
            if (this.readOnly) {
                addTrack.Hide();
            } else {
                addTrack.Show();
            }
            playPosition.Show();
        } else {
            lenHb.Hide();
            addTrack.Hide();
            playPosition.Hide();
        }
        QueueRedraw();
    }

    public void SetHscroll(HScrollBar hscroll) {
        this.hscroll = hscroll;
    }


    public void AutoFit() {
        if (animation is null) {
            return;
        }

        float animEnd = animation.Length;
        float animStart = 0;

        // Search for keyframe outside animation boundaries to include keyframes before animation start and after animation length.
        int trackCount = animation.GetTrackCount();
        for (int track = 0; track < trackCount; ++track) {
            for (int i = 0; i < animation.TrackGetKeyCount(track); i++) {
                float keyTime = (float)animation.TrackGetKeyTime(track, i);
                if (keyTime > animEnd) {
                    animEnd = keyTime;
                }
                if (keyTime < animStart) {
                    animStart = keyTime;
                }
            }
        }

        float animLength = animEnd - animStart;
        int timelineWidthPixels = (int)Size.X - ButtonsWidth - NameLimit;

        // I want a little buffer at the end... (5% looks nice and we should keep some space for the bezier handles)
        timelineWidthPixels = (int)(timelineWidthPixels * 0.95f);

        // The technique is to reuse the _get_zoom_scale function directly to be sure that the auto_fit is always calculated
        // the same way as the zoom slider. It's a little bit more calculation then doing the inverse of get_zoom_scale but
        // it's really easier to understand and should always be accurate.
        float newZoom = (float)_zoom.MaxValue;
        while (true) {
            double testZoomScale = GetZoomScale(newZoom);

            if (animLength * testZoomScale <= timelineWidthPixels) {
                // It fits...
                break;
            }

            newZoom -= (float)_zoom.Step;

            if (newZoom <= _zoom.MinValue) {
                newZoom = (float)_zoom.MaxValue;
                break;
            }
        }

        // Horizontal scroll to get_min which should include keyframes that are before the animation start.
        hscroll.Value = hscroll.MinValue;
        // Set the zoom value... the signal value_changed will be emitted and the timeline will be refreshed correctly!
        _zoom.Value = newZoom;
        // The new zoom value must be applied correctly so the scrollbar are updated before we move the scrollbar to
        // the beginning of the animation, hence the call deferred.
        new Callable(this, MethodName._ScrollToStart).CallDeferred();
    }

    public void UpdatePlayPosition() {
        playPosition.QueueRedraw();
    }

    public void UpdateValues() {
        if (animation is null || editing) {
            return;
        }

        editing = true;
        if (_useFps && animation.Step > 0.0) {
            length.Value = animation.Length / animation.Step;
            length.Step = FpsDecimal;
            length.TooltipText = Tr("Animation length (frames)");
            timeIcon.TooltipText = Tr("Animation length (frames)");
            // if (track_edit is not null) {
            //     track_edit.editor._update_key_edit();
            //     track_edit.editor.marker_edit._update_key_edit();
            // }
        } else {
            length.Value = animation.Length;
            length.Step = SecondDecimal;
            length.TooltipText = Tr("Animation length (seconds)");
            timeIcon.TooltipText = Tr("Animation length (seconds)");
        }

        switch (animation.LoopMode) {
            case Animation.LoopModeEnum.None: {
                loop.Icon = GetThemeIcon("Loop");
                loop.ButtonPressed = false;
            }
            break;
            case Animation.LoopModeEnum.Linear: {
                loop.Icon = GetThemeIcon("Loop");
                loop.ButtonPressed = true;
            }
            break;
            case Animation.LoopModeEnum.Pingpong: {
                loop.Icon = GetThemeIcon("PingPongLoop");
                loop.ButtonPressed = true;
            }
            break;
            default:
                break;
        }

        editing = false;
    }

    private float GetZoomScale(double zoomValue) {
        float zv = (float)(_zoom.MaxValue - zoomValue);
        if (zv < 1) {
            zv = 1f - zv;
            return Mathf.Pow(1f + zv, 8f) * 100;
        } else {
            return 1f / Mathf.Pow(zv, 8f) * 100;
        }
    }

    // https://stackoverflow.com/a/2319692
    // I see
    private void _ZoomChanged(double _) {
        double zoomPivot = 0; // Point on timeline to stay fixed.
        double zoomPivotDelta = 0; // Delta seconds from left-most point on timeline to zoom pivot.

        int timelineWidthPixels = (int)Size.X - ButtonsWidth - NameLimit;
        double timelineWidthSeconds = timelineWidthPixels / lastZoomScale; // Length (in seconds) of visible part of timeline before zoom.
        double updatedTimelineWidthSeconds = timelineWidthPixels / ZoomScale; // Length after zoom.
        double updatedTimelineHalfWidth = updatedTimelineWidthSeconds / 2.0;
        bool zooming = updatedTimelineWidthSeconds < timelineWidthSeconds;

        double timelineLeft = Value;
        double timelineRight = timelineLeft + timelineWidthSeconds;
        double timelineCenter = timelineLeft + timelineWidthSeconds / 2.0;

        if (zoomCallbackOccured) { // Zooming with scroll wheel will focus on the position of the mouse.
            double zoomScrollOriginNorm = (zoomScrollOrigin.X - NameLimit) / timelineWidthPixels;
            zoomScrollOriginNorm = Mathf.Max(zoomScrollOriginNorm, 0);
            zoomPivot = timelineLeft + timelineWidthSeconds * zoomScrollOriginNorm;
            zoomPivotDelta = updatedTimelineWidthSeconds * zoomScrollOriginNorm;
            zoomCallbackOccured = false;
        } else { // Zooming with slider will depend on the current play position.
                 // If the play position is not in range, or exactly in the center, zoom in on the center.
            if (PlayPosition < timelineLeft || PlayPosition > timelineLeft + timelineWidthSeconds || PlayPosition == timelineCenter) {
                zoomPivot = timelineCenter;
                zoomPivotDelta = updatedTimelineHalfWidth;
            }
            // Zoom from right if play position is right of center,
            // and shrink from right if play position is left of center.
            else if ((PlayPosition > timelineCenter) == zooming) {
                // If play position crosses to other side of center, center it.
                bool centerPassed = (PlayPosition < timelineRight - updatedTimelineHalfWidth) == zooming;
                zoomPivot = centerPassed ? PlayPosition : timelineRight;
                double centerSffset = double.Epsilon * (zooming ? 1 : -1); // Small offset to prevent crossover.
                zoomPivotDelta = centerPassed ? updatedTimelineHalfWidth + centerSffset : updatedTimelineWidthSeconds;
            }
            // Zoom from left if play position is left of center,
            // and shrink from left if play position is right of center.
            else if ((PlayPosition <= timelineCenter) == zooming) {
                // If play position crosses to other side of center, center it.
                bool centerPassed = (PlayPosition > timelineLeft + updatedTimelineHalfWidth) == zooming;
                zoomPivot = centerPassed ? PlayPosition : timelineLeft;
                double centerOffset = double.Epsilon * (zooming ? -1 : 1); // Small offset to prevent crossover.
                zoomPivotDelta = centerPassed ? updatedTimelineHalfWidth + centerOffset : 0;
            }
        }

        double hscrollPos = zoomPivot - zoomPivotDelta;
        hscrollPos = Mathf.Clamp(hscrollPos, hscroll.MinValue, hscroll.MaxValue);

        hscroll.Value = hscrollPos;
        hscrollOnZoomBuffer = hscrollPos; // In case of page update.
        lastZoomScale = ZoomScale;

        QueueRedraw();
        // play_position.queue_redraw();
        EmitSignal(SignalName.ZoomChanged);
    }

    private void _AnimNameChanged(double newLen) {
        if (editing) {
            return;
        }

        newLen = Mathf.Max(SecondDecimal, newLen);
        if (_useFps && animation.Step > 0) {
            newLen *= animation.Step;
        }

        editing = true;
        // EditorUndoRedoManager *undo_redo = EditorUndoRedoManager::get_singleton();
        // undo_redo.create_action(Tr("Change Animation Length"), UndoRedo::MERGE_ENDS);
        // undo_redo.add_do_method(animation.ptr(), "set_length", p_new_len);
        // undo_redo.add_undo_method(animation.ptr(), "set_length",animation.Length);
        // undo_redo.commit_action();
        editing = false;
        QueueRedraw();

        EmitSignal(SignalName.LengthChanged, newLen);
    }

    private void _AnimLoopPressed() {
        if (!readOnly) {
            // EditorUndoRedoManager *undo_redo = EditorUndoRedoManager::get_singleton();
            // undo_redo.create_action(Tr("Change Animation Loop"));
            // switch (animation.get_loop_mode()) {
            //     case Animation::LOOP_NONE: {
            //         undo_redo.add_do_method(animation.ptr(), "set_loop_mode", Animation::LOOP_LINEAR);
            //     }
            //     break;
            //     case Animation::LOOP_LINEAR: {
            //         undo_redo.add_do_method(animation.ptr(), "set_loop_mode", Animation::LOOP_PINGPONG);
            //     }
            //     break;
            //     case Animation::LOOP_PINGPONG: {
            //         undo_redo.add_do_method(animation.ptr(), "set_loop_mode", Animation::LOOP_NONE);
            //     }
            //     break;
            //     default:
            //         break;
            // }
            // undo_redo.add_do_method(this, "update_values");
            // undo_redo.add_undo_method(animation.ptr(), "set_loop_mode", animation.get_loop_mode());
            // undo_redo.add_undo_method(this, "update_values");
            // undo_redo.commit_action();
        } else {
            string basePath = animation.ResourcePath;
            if (FileAccess.FileExists(basePath + ".import")) {
                OS.Alert(Tr("Can't change loop mode on animation instanced from imported scene."));
            } else {
                OS.Alert(Tr("Can't change loop mode on animation embedded in another scene."));
            }
            UpdateValues();
        }
    }

    public override void _Draw() {
        int keyRange = (int)Size.X - ButtonsWidth - NameLimit;

        if (animation is null) {
            return;
        }

        Font font = GetThemeFont("font", "Label");
        int fontSize = GetThemeFontSize("font_size", "Label");

        StyleBox styleboxTimeUnavailable = GetThemeStylebox("time_unavailable", "AnimationTimelineEdit");
        StyleBox styleboxTimeAvailable = GetThemeStylebox("time_available", "AnimationTimelineEdit");

        Color vLinePrimaryColor = GetThemeColor("v_line_primary_color", "AnimationTimelineEdit");
        Color vLineSecondaryColor = GetThemeColor("v_line_secondary_color", "AnimationTimelineEdit");
        Color hLineColor = GetThemeColor("h_line_color", "AnimationTimelineEdit");
        Color fontPrimaryColor = GetThemeColor("font_primary_color", "AnimationTimelineEdit");
        Color fontSecondaryColor = GetThemeColor("font_secondary_color", "AnimationTimelineEdit");

        int vLinePrimaryMargin = GetThemeConstant("v_line_primary_margin", "AnimationTimelineEdit");
        int vLineSecondaryMargin = GetThemeConstant("v_line_secondary_margin", "AnimationTimelineEdit");
        int vLinePrimaryWidth = GetThemeConstant("v_line_primary_width", "AnimationTimelineEdit");
        int vLineSecondaryWidth = GetThemeConstant("v_line_secondary_width", "AnimationTimelineEdit");
        int textPrimaryMargin = GetThemeConstant("text_primary_margin", "AnimationTimelineEdit");
        int textSecondaryMargin = GetThemeConstant("text_secondary_margin", "AnimationTimelineEdit");

        int zoomw = keyRange;
        float scale = ZoomScale;
        int h = (int)Size.Y;

        float l = animation.Length;
        if (l <= 0) {
            l = (float)SecondDecimal; // Avoid crashor.
        }

        Texture2D hsizeIcon = GetThemeIcon("Hsize");
        hsizeRect = new(NameLimit - hsizeIcon.GetWidth() - 8 * EDSCALE, (Size.Y - hsizeIcon.GetHeight()) / 2, hsizeIcon.GetWidth(), hsizeIcon.GetHeight());
        DrawTexture(hsizeIcon, hsizeRect.Position);

        {
            float timeMin = 0;
            float timeMax = animation.Length;
            for (int i = 0; i < animation.GetTrackCount(); i++) {
                if (animation.TrackGetKeyCount(i) > 0) {
                    float beg = (float)animation.TrackGetKeyTime(i, 0);

                    if (beg < timeMin) {
                        timeMin = beg;
                    }

                    float end = (float)animation.TrackGetKeyTime(i, animation.TrackGetKeyCount(i) - 1);

                    if (end > timeMax) {
                        timeMax = end;
                    }
                }
            }

            string[] markers = animation.GetMarkerNames();
            if (markers.Length > 0) {
                float minMarker = (float)animation.GetMarkerTime(markers[0]);
                float maxMarker = (float)animation.GetMarkerTime(markers[^1]);
                if (minMarker < timeMin) {
                    timeMin = minMarker;
                }
                if (maxMarker > timeMax) {
                    timeMax = maxMarker;
                }
            }

            float extra = zoomw / scale * 0.5f;

            timeMax += extra;
            MinValue = timeMin;
            MaxValue = timeMax;

            if (zoomw / scale < (timeMax - timeMin)) {
                hscroll.Show();

            } else {
                hscroll.Hide();
            }
        }

        Page = zoomw / scale;

        if (hscroll.Visible && hscrollOnZoomBuffer >= 0) {
            hscroll.Value = hscrollOnZoomBuffer;
            hscrollOnZoomBuffer = -1.0;
        }

        int endPx = (int)((l - Value) * scale);
        int beginPx = (int)(-Value * scale);

        {
            DrawStyleBox(styleboxTimeUnavailable, new(new(NameLimit, 0), new(zoomw - 1f, h)));

            if (beginPx < zoomw && endPx > 0) {
                if (beginPx < 0) {
                    beginPx = 0;
                }
                if (endPx > zoomw) {
                    endPx = zoomw;
                }

                DrawStyleBox(styleboxTimeAvailable, new(new(NameLimit + beginPx, 0), new(endPx - beginPx, h)));
            }
        }

        int dec = 1;
        int step = 1;
        int decimals = 2;
        bool stepFound = false;

        float periodWidth = font.GetCharSize('.', fontSize).X;
        float maxDigitWidth = font.GetCharSize('0', fontSize).X;
        for (int i = 1; i <= 9; i++) {
            float digitWidth = font.GetCharSize('0' + i, fontSize).X;
            maxDigitWidth = Mathf.Max(digitWidth, maxDigitWidth);
        }
        int maxSc = Mathf.CeilToInt(zoomw / scale);
        int maxScWidth = maxSc.ToString().Length * Mathf.CeilToInt(maxDigitWidth);

        int minMargin = Mathf.Max(textSecondaryMargin, textPrimaryMargin);

        while (!stepFound) {
            int min = maxScWidth;
            if (decimals > 0) {
                min += Mathf.CeilToInt(periodWidth + maxDigitWidth * decimals);
            }

            min += minMargin * 2;

            int[] _multp = [ 1, 2, 5 ];
            for (int i = 0; i < 3; i++) {
                step = _multp[i] * dec;
                if (step * scale / ScAdj > min) {
                    stepFound = true;
                    break;
                }
            }
            if (stepFound) {
                break;
            }
            dec *= 10;
            decimals--;
            if (decimals < 0) {
                decimals = 0;
            }
        }

        if (_useFps) {
            float stepSize = animation.Step;
            if (stepSize > 0) {
                int prevFrameOfs = -10000000;

                for (int i = 0; i < zoomw; i++) {
                    float pos = (float)(Value + i / scale);
                    float prev = (float)(Value + (i - 1.0) / scale);

                    int frame = (int)(pos / stepSize);
                    int prevFrame = (int)(prev / stepSize);

                    bool sub = Mathf.Floor(prev) == Mathf.Floor(pos);

                    if (frame != prevFrame && i >= prevFrameOfs) {
                        int lineMargin = sub ? vLineSecondaryMargin : vLinePrimaryMargin;
                        int lineWidth = sub ? vLineSecondaryWidth : vLinePrimaryWidth;
                        Color lineColor = sub ? vLineSecondaryColor : vLinePrimaryColor;

                        DrawLine(new(NameLimit + i, 0 + lineMargin), new(NameLimit + i, h - lineMargin), lineColor, lineWidth);

                        int textMargin = sub ? textSecondaryMargin : textPrimaryMargin;

                        DrawString(font, new Vector2(NameLimit + i + textMargin, (h - font.GetHeight(fontSize)) / 2 + font.GetAscent(fontSize)).Floor(), frame.ToString(), HorizontalAlignment.Left, zoomw - i, fontSize, sub ? fontSecondaryColor : fontPrimaryColor);

                        prevFrameOfs = i + (int)font.GetStringSize(frame.ToString(), HorizontalAlignment.Left, -1, fontSize).X + textMargin;
                    }
                }
            }

        } else {
            for (int i = 0; i < zoomw; i++) {
                float pos = (float)(Value + i / scale);
                float prev = (float)(Value + (i - 1.0) / scale);

                int sc = Mathf.FloorToInt(pos * ScAdj);
                int prevSc = Mathf.FloorToInt(prev * ScAdj);

                if ((sc / step) != (prevSc / step) || (prevSc < 0 && sc >= 0)) {
                    int scd = sc < 0 ? prevSc : sc;
                    bool sub = ((scd - (scd % step)) % (dec * 10)) != 0;

                    int lineMargin = sub ? vLineSecondaryMargin : vLinePrimaryMargin;
                    int lineWidth = sub ? vLineSecondaryWidth : vLinePrimaryWidth;
                    Color lineColor = sub ? vLineSecondaryColor : vLinePrimaryColor;

                    DrawLine(new(NameLimit + i, 0 + lineMargin), new(NameLimit + i, h - lineMargin), lineColor, lineWidth);

                    int textMargin = sub ? textSecondaryMargin : textPrimaryMargin;

                    DrawString(font, new Vector2(NameLimit + i + textMargin, (h - font.GetHeight(fontSize)) / 2 + font.GetAscent(fontSize)).Floor(), ((scd - (scd % step)) / (double)ScAdj).ToString($"0.{decimals}f", System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, zoomw - i, fontSize, sub ? fontSecondaryColor : fontPrimaryColor);
                }
            }
        }

        DrawLine(new(0, Size.Y), Size, hLineColor, Mathf.Round(EDSCALE));
        UpdateValues();
    }

    public override void _Notification(int what) {
        switch ((long)what) {
            case NotificationEnterTree:
            case NotificationThemeChanged: {
                addTrack.Icon = GetThemeIcon("Add");
                // loop.Icon = GetThemeIcon("Loop");
                timeIcon.Texture = GetThemeIcon("Time");

                addTrack.GetPopup().Clear();
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyValue"), Tr("Property Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyXPosition"), Tr("3D Position Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyXRotation"), Tr("3D Rotation Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyXScale"), Tr("3D Scale Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyBlendShape"), Tr("Blend Shape Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyCall"), Tr("Call Method Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyBezier"), Tr("Bezier Curve Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyAudio"), Tr("Audio Playback Track..."));
                addTrack.GetPopup().AddIconItem(GetThemeIcon("KeyAnimation"), Tr("Animation Playback Track..."));
            }
            break;

            case EditorSettings.NotificationEditorSettingsChanged: {
                // if (EditorSettings::get_singleton().check_changed_settings_in_group("editors/panning")) {
                //     panner->setup((ViewPanner::ControlScheme)EDITOR_GET("editors/panning/animation_editors_panning_scheme").operator int(), ED_GET_SHORTCUT("canvas_item_editor/pan_view"), bool(EDITOR_GET("editors/panning/simple_panning")));
                // }
                panner.Setup(ViewPanner.ControlSchemeEnum.ScrollPans, new Shortcut() { Events = [new InputEventKey() { Keycode = Key.Space }] }, false);
            }
            break;

            case NotificationResized: {
                lenHb.Position = new(Size.X - ButtonsWidth, 0);
                lenHb.Size = new(ButtonsWidth, Size.Y);
            }
            break;

            case NotificationDraw: {
                int keyRange = (int)Size.X - ButtonsWidth - NameLimit;

                if (animation is null) {
                    return;
                }

                Font font = GetThemeFont("font", "Label");
                int fontSize = GetThemeFontSize("font_size", "Label");

                StyleBox styleboxTimeUnavailable = GetThemeStylebox("time_unavailable", "AnimationTimelineEdit");
                StyleBox styleboxTimeAvailable = GetThemeStylebox("time_available", "AnimationTimelineEdit");

                Color vLinePrimaryColor = GetThemeColor("v_line_primary_color", "AnimationTimelineEdit");
                Color vLineSecondaryColor = GetThemeColor("v_line_secondary_color", "AnimationTimelineEdit");
                Color hLineColor = GetThemeColor("h_line_color", "AnimationTimelineEdit");
                Color fontPrimaryColor = GetThemeColor("font_primary_color", "AnimationTimelineEdit");
                Color fontSecondaryColor = GetThemeColor("font_secondary_color", "AnimationTimelineEdit");

                int vLinePrimaryMargin = GetThemeConstant("v_line_primary_margin", "AnimationTimelineEdit");
                int vLineSecondaryMargin = GetThemeConstant("v_line_secondary_margin", "AnimationTimelineEdit");
                int vLinePrimaryWidth = GetThemeConstant("v_line_primary_width", "AnimationTimelineEdit");
                int vLineSecondaryWidth = GetThemeConstant("v_line_secondary_width", "AnimationTimelineEdit");
                int textPrimaryMargin = GetThemeConstant("text_primary_margin", "AnimationTimelineEdit");
                int textSecondaryMargin = GetThemeConstant("text_secondary_margin", "AnimationTimelineEdit");

                int zoomw = keyRange;
                float scale = ZoomScale;
                int h = (int)Size.Y;

                float l = animation.Length;
                if (l <= 0) {
                    l = (float)SecondDecimal; // Avoid crashor.
                }

                Texture2D hsizeIcon = GetThemeIcon("Hsize");
                hsizeRect = new(NameLimit - hsizeIcon.GetWidth() - 8 * EDSCALE, (Size.Y - hsizeIcon.GetHeight()) / 2, hsizeIcon.GetWidth(), hsizeIcon.GetHeight());
                DrawTexture(hsizeIcon, hsizeRect.Position);

                {
                    float timeMin = 0;
                    float timeMax = animation.Length;
                    for (int i = 0; i < animation.GetTrackCount(); i++) {
                        if (animation.TrackGetKeyCount(i) > 0) {
                            float beg = (float)animation.TrackGetKeyTime(i, 0);

                            if (beg < timeMin) {
                                timeMin = beg;
                            }

                            float end = (float)animation.TrackGetKeyTime(i, animation.TrackGetKeyCount(i) - 1);

                            if (end > timeMax) {
                                timeMax = end;
                            }
                        }
                    }

                    string[] markers = animation.GetMarkerNames();
                    if (markers.Length > 0) {
                        float minMarker = (float)animation.GetMarkerTime(markers[0]);
                        float maxMarker = (float)animation.GetMarkerTime(markers[^1]);
                        if (minMarker < timeMin) {
                            timeMin = minMarker;
                        }
                        if (maxMarker > timeMax) {
                            timeMax = maxMarker;
                        }
                    }

                    float extra = zoomw / scale * 0.5f;

                    timeMax += extra;
                    MinValue = timeMin;
                    MaxValue = timeMax;

                    if (zoomw / scale < (timeMax - timeMin)) {
                        hscroll.Show();

                    } else {
                        hscroll.Hide();
                    }
                }

                Page = zoomw / scale;

                if (hscroll.Visible && hscrollOnZoomBuffer >= 0) {
                    hscroll.Value = hscrollOnZoomBuffer;
                    hscrollOnZoomBuffer = -1.0;
                }

                int endPx = (int)((l - Value) * scale);
                int beginPx = (int)(-Value * scale);

                {
                    DrawStyleBox(styleboxTimeUnavailable, new(new(NameLimit, 0), new(zoomw - 1f, h)));

                    if (beginPx < zoomw && endPx > 0) {
                        if (beginPx < 0) {
                            beginPx = 0;
                        }
                        if (endPx > zoomw) {
                            endPx = zoomw;
                        }

                        DrawStyleBox(styleboxTimeAvailable, new(new(NameLimit + beginPx, 0), new(endPx - beginPx, h)));
                    }
                }

                int dec = 1;
                int step = 1;
                int decimals = 2;
                bool stepFound = false;

                float periodWidth = font.GetCharSize('.', fontSize).X;
                float maxDigitWidth = font.GetCharSize('0', fontSize).X;
                for (int i = 1; i <= 9; i++) {
                    float digitWidth = font.GetCharSize('0' + i, fontSize).X;
                    maxDigitWidth = Mathf.Max(digitWidth, maxDigitWidth);
                }
                int maxSc = Mathf.CeilToInt(zoomw / scale);
                int maxScWidth = maxSc.ToString().Length * Mathf.CeilToInt(maxDigitWidth);

                int minMargin = Mathf.Max(textSecondaryMargin, textPrimaryMargin);

                while (!stepFound) {
                    int min = maxScWidth;
                    if (decimals > 0) {
                        min += Mathf.CeilToInt(periodWidth + maxDigitWidth * decimals);
                    }

                    min += minMargin * 2;

                    int[] _multp = [ 1, 2, 5 ];
                    for (int i = 0; i < 3; i++) {
                        step = _multp[i] * dec;
                        if (step * scale / ScAdj > min) {
                            stepFound = true;
                            break;
                        }
                    }
                    if (stepFound) {
                        break;
                    }
                    dec *= 10;
                    decimals--;
                    if (decimals < 0) {
                        decimals = 0;
                    }
                }

                if (_useFps) {
                    float stepSize = animation.Step;
                    if (stepSize > 0) {
                        int prevFrameOfs = -10000000;

                        for (int i = 0; i < zoomw; i++) {
                            float pos = (float)(Value + i / scale);
                            float prev = (float)(Value + (i - 1.0) / scale);

                            int frame = (int)(pos / stepSize);
                            int prevFrame = (int)(prev / stepSize);

                            bool sub = Mathf.Floor(prev) == Mathf.Floor(pos);

                            if (frame != prevFrame && i >= prevFrameOfs) {
                                int lineMargin = sub ? vLineSecondaryMargin : vLinePrimaryMargin;
                                int lineWidth = sub ? vLineSecondaryWidth : vLinePrimaryWidth;
                                Color lineColor = sub ? vLineSecondaryColor : vLinePrimaryColor;

                                DrawLine(new(NameLimit + i, 0 + lineMargin), new(NameLimit + i, h - lineMargin), lineColor, lineWidth);

                                int textMargin = sub ? textSecondaryMargin : textPrimaryMargin;

                                DrawString(font, new Vector2(NameLimit + i + textMargin, (h - font.GetHeight(fontSize)) / 2 + font.GetAscent(fontSize)).Floor(), frame.ToString(), HorizontalAlignment.Left, zoomw - i, fontSize, sub ? fontSecondaryColor : fontPrimaryColor);

                                prevFrameOfs = i + (int)font.GetStringSize(frame.ToString(), HorizontalAlignment.Left, -1, fontSize).X + textMargin;
                            }
                        }
                    }

                } else {
                    for (int i = 0; i < zoomw; i++) {
                        float pos = (float)(Value + i / scale);
                        float prev = (float)(Value + (i - 1.0) / scale);

                        int sc = Mathf.FloorToInt(pos * ScAdj);
                        int prevSc = Mathf.FloorToInt(prev * ScAdj);

                        if ((sc / step) != (prevSc / step) || (prevSc < 0 && sc >= 0)) {
                            int scd = sc < 0 ? prevSc : sc;
                            bool sub = ((scd - (scd % step)) % (dec * 10)) != 0;

                            int lineMargin = sub ? vLineSecondaryMargin : vLinePrimaryMargin;
                            int lineWidth = sub ? vLineSecondaryWidth : vLinePrimaryWidth;
                            Color lineColor = sub ? vLineSecondaryColor : vLinePrimaryColor;

                            DrawLine(new(NameLimit + i, 0 + lineMargin), new(NameLimit + i, h - lineMargin), lineColor, lineWidth);

                            int textMargin = sub ? textSecondaryMargin : textPrimaryMargin;

                            DrawString(font, new Vector2(NameLimit + i + textMargin, (h - font.GetHeight(fontSize)) / 2 + font.GetAscent(fontSize)).Floor(), ((scd - (scd % step)) / (double)ScAdj).ToString($"0.{decimals}f", System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, zoomw - i, fontSize, sub ? fontSecondaryColor : fontPrimaryColor);
                        }
                    }
                }

                DrawLine(new(0, Size.Y), Size, hLineColor, Mathf.Round(EDSCALE));
                UpdateValues();
                DrawRect(new(new(), Size), Colors.Yellow, false);
            }
            break;
            // case 1005 or NotificationWMWindowFocusOut: // the exact same
            //     throw new Exception("an exception has been thrown or it would freeze");
            //     // break;
        }
    }

    private void PlayPositionDraw() {
        if (animation is null || playPositionPos < 0) {
            return;
        }

        float scale = ZoomScale;
        int h = (int)playPosition.Size.Y;

        int px = (int)((-Value + playPositionPos) * scale + NameLimit);

        if (px >= NameLimit && px < (playPosition.Size.X - ButtonsWidth)) {
            Color color = GetThemeColor("accent_color", "Editor");
            playPosition.DrawLine(new(px, 0), new(px, h), color, Mathf.Round(2 * EDSCALE));
            playPosition.DrawTexture(
                    GetThemeIcon("TimelineIndicator"),
                    new(px - GetThemeIcon("TimelineIndicator").GetWidth() * 0.5f, 0),
                    color);
        }
    }

    private void Gui_Input(InputEvent @event) {
        if (@event is null)
            throw new NullReferenceException($"{nameof(@event)} is null");

        if (panner.GuiInput(@event)) {
            AcceptEvent();
            return;
        }

        if (@event is InputEventMouseButton mb) {
            if (mb.Pressed && mb.AltPressed && mb.ButtonIndex == MouseButton.WheelUp) {
                // if (track_edit) {
                //     track_edit.get_editor().goto_prev_step(true);
                // }
                AcceptEvent();
            }

            if (mb.Pressed && mb.AltPressed && mb.ButtonIndex == MouseButton.WheelDown) {
                // if (track_edit) {
                //     track_edit.get_editor().goto_next_step(true);
                // }
                AcceptEvent();
            }

            if (mb.Pressed && mb.ButtonIndex == MouseButton.Left && hsizeRect.HasPoint(mb.Position)) {
                draggingHsize = true;
                draggingHsizeFrom = mb.Position.X;
                draggingHsizeAt = nameLimit;
            }

            if (!mb.Pressed && mb.ButtonIndex == MouseButton.Left && draggingHsize) {
                draggingHsize = false;
            }
            if (mb.Position.X > NameLimit && mb.Position.X < (Size.X - ButtonsWidth)) {
                if (!panner.Panning && mb.ButtonIndex == MouseButton.Left) {
                    int x = (int)mb.Position.X - NameLimit;

                    float ofs = x / ZoomScale + (float)Value;
                    EmitSignal(SignalName.TimelineChanged, ofs, mb.AltPressed);
                    draggingTimeline = true;
                }
            }

            if (draggingTimeline && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) {
                draggingTimeline = false;
            }
        }


        if (@event is InputEventMouseMotion mm) {
            if (draggingHsize) {
                int ofs = (int)(mm.Position.X - draggingHsizeFrom);
                nameLimit = (int)draggingHsizeAt + ofs;
                // Make sure name_limit is clamped to the range that UI allows.
                nameLimit = NameLimit;
                QueueRedraw();
                EmitSignal(SignalName.NameLimitChanged);
                playPosition.QueueRedraw();
            }
            if (draggingTimeline) {
                int x = (int)mm.Position.X - NameLimit;
                float ofs = x / ZoomScale + (float)Value;
                EmitSignal(SignalName.TimelineChanged, ofs, mm.AltPressed);
            }
        }
    }
    private void _TrackAdded(long p_track) {
        EmitSignal(SignalName.TrackAdded, p_track);
    }
    private void _ScrollToStart() {
        // Horizontal scroll to get_min which should include keyframes that are before the animation start.
        hscroll.Value = hscroll.MinValue;
    }

    // void set_track_edit(AnimationTrackEdit *p_track_edit) {}

    public void PanCallback(Vector2 pScrollVec, InputEvent @event) {
        Value -= pScrollVec.X / ZoomScale;
    }
    public void ZoomCallback(float zoomFactor, Vector2 origin, InputEvent @event) {
        double currentZoomValue = Zoom.Value;
        zoomScrollOrigin = origin;
        zoomCallbackOccured = true;
        Zoom.Value = Mathf.Max(0.01, currentZoomValue - (1.0 - zoomFactor));
    }
}