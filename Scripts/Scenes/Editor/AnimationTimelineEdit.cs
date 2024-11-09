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

[GlobalClass]
public partial class AnimationTimelineEdit : Range {
    [Signal]
    public delegate void ZoomChangedEventHandler();
    [Signal]
    public delegate void LengthChangedEventHandler(double newLength);
    [Signal]
    public delegate void TimelineChangedEventHandler(float offset, bool altPressed);
    [Signal]
    public delegate void NameLimitChangedEventHandler();
    public const float SCROLL_ZOOM_FACTOR_IN = 1.02f; // Zoom factor per mouse scroll in the animation editor when zooming in. The closer to 1.0, the finer the control.
    public const float SCROLL_ZOOM_FACTOR_OUT = 0.98f; // Zoom factor when zooming out. Similar to SCROLL_ZOOM_FACTOR_IN but less than 1.0.
    public const double FPS_DECIMAL = 1.0;
    public const double SECOND_DECIMAL = 0.0001;
    public const double FPS_STEP_FRACTION = 0.0625;
    private const float SC_ADJ = 100f;
    public const float EDSCALE = 1f;

    private Animation animation;
    private bool read_only = false;

    // AnimationTrackEdit *track_edit;
    private int name_limit = 0;
    private Range zoom;
    private Range h_scroll;
    float play_position_pos = 0.0f;

    private HBoxContainer len_hb;
    private SpinSlider length;
    private Button loop;
    private TextureRect time_icon;

    private MenuButton add_track;
    private Control play_position; //separate control used to draw so updates for only position changed are much faster
    private HScrollBar hscroll;

    private Rect2 hsize_rect;

    private bool editing = false;
    private bool use_fps = false;

    private ViewPanner panner;

    private bool dragging_timeline = false;
    private bool dragging_hsize = false;
    private float dragging_hsize_from = 0.0f;
    private float dragging_hsize_at = 0.0f;
    private double last_zoom_scale = 1.0;
    private double hscroll_on_zoom_buffer = -1.0;

    private Vector2 zoom_scroll_origin;
    private bool zoom_callback_occured = false;
    public AnimationTimelineEdit() {
        name_limit = (int)(150 * EDSCALE);

        play_position = new() {
            MouseFilter = MouseFilterEnum.Pass
        };
        AddChild(play_position);
        play_position.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        play_position.Draw += _PlayPositionDraw;

        add_track = new() {
            Position = Vector2.Zero,
            Text = Tr("Add Track")
        };
        AddChild(add_track);

        len_hb = new();

        Control expander = new() {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        len_hb.AddChild(expander);
        time_icon = new() {
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = Tr("Animation length (seconds)")
        };
        len_hb.AddChild(time_icon);
        length = new() {
            MinValue = SECOND_DECIMAL,
            MaxValue = 36000,
            Step = SECOND_DECIMAL,
            AllowGreater = true,
            CustomMinimumSize = new(70 * EDSCALE, 0),
            HideSlider = true,
            TooltipText = Tr("Animation length (seconds)")
        };
        length.ValueChanged += _AnimLengthChanged;
        len_hb.AddChild(length);
        loop = new() {
            Flat = true,
            TooltipText = Tr("Animation Looping"),
            ToggleMode = true
        };
        loop.Pressed += _AnimLoopPressed;
        len_hb.AddChild(loop);
        AddChild(len_hb);

        add_track.Hide();
        add_track.GetPopup().IndexPressed += _TrackAdded;
        len_hb.Hide();

        panner = new();
        panner.SetScrollZoomFactor(SCROLL_ZOOM_FACTOR_IN);
        panner.SetCallbacks(_PanCallback, _ZoomCallback);
        panner.PanAxis = ViewPanner.PanAxisEnum.Horizontal;

        LayoutDirection = LayoutDirectionEnum.Ltr;
    }

    public float ZoomScale => GetZoomScale(zoom.Value);
    public int ButtonsWidth {
        get {
            Texture2D interp_mode = GetThemeIcon("TrackContinuous");
            Texture2D interp_type = GetThemeIcon("InterpRaw");
            Texture2D loop_type = GetThemeIcon("InterpWrapClamp");
            Texture2D remove_icon = GetThemeIcon("Remove");
            Texture2D down_icon = GetThemeIcon("select_arrow", "Tree");

            int h_separation = GetThemeConstant("h_separation", "AnimationTrackEdit");

            int total_w = interp_mode.GetWidth() + interp_type.GetWidth() + loop_type.GetWidth() + remove_icon.GetWidth();
            total_w += (down_icon.GetWidth() + h_separation) * 4;

            return total_w;
        }
    }
    public int NameLimit {
        get {
            Texture2D hsize_icon = GetThemeIcon("Hsize");

            int limit = (int)Mathf.Max(name_limit, add_track.GetMinimumSize().X + hsize_icon.GetWidth() + 8 * EDSCALE);

            limit = Mathf.Min(limit, (int)Size.X - ButtonsWidth - 1);

            return limit;
        }
    }

    public Range Zoom {
        get => zoom;
        set {
            zoom = value;
            zoom.ValueChanged += _ZoomChanged;
        }
    }

    public float PlayPosition {
        get => play_position_pos;
        set {
            play_position_pos = value;
            QueueRedraw();
        }
    }

    public bool UseFps {
        get => use_fps;
        set {
            use_fps = value;
            QueueRedraw();
        }
    }

    public override Vector2 _GetMinimumSize() {
        Vector2 ms = add_track.GetMinimumSize();
        Font font = GetThemeFont("font", "Label");
        int font_size = GetThemeFontSize("font_size", "Label");
        ms.Y = Mathf.Max(ms.Y, font.GetHeight(font_size));
        ms.X = ButtonsWidth + add_track.GetMinimumSize().X + GetThemeIcon("Hsize").GetWidth() + 2 + 8 * EDSCALE;
        return ms;
    }
    public void SetAnimation(Animation p_animation, bool p_read_only) {
        animation = p_animation;
        read_only = p_read_only;

        length.ReadOnly = read_only;

        if (animation is not null) {
            len_hb.Show();
            if (read_only) {
                add_track.Hide();
            } else {
                add_track.Show();
            }
            play_position.Show();
        } else {
            len_hb.Hide();
            add_track.Hide();
            play_position.Hide();
        }
        QueueRedraw();
    }
    // public void set_track_edit(AnimationTrackEdit p_track_edit) {}
    public void AutoFit() {
        if (animation is null) {
            return;
        }

        float anim_end = animation.Length;
        float anim_start = 0;

        // Search for keyframe outside animation boundaries to include keyframes before animation start and after animation length.
        int track_count = animation.GetTrackCount();
        for (int track = 0; track < track_count; ++track) {
            for (int i = 0; i < animation.TrackGetKeyCount(track); i++) {
                float key_time = (float)animation.TrackGetKeyTime(track, i);
                if (key_time > anim_end) {
                    anim_end = key_time;
                }
                if (key_time < anim_start) {
                    anim_start = key_time;
                }
            }
        }

        float anim_length = anim_end - anim_start;
        int timeline_width_pixels = (int)Size.X - ButtonsWidth - NameLimit;

        // I want a little buffer at the end... (5% looks nice and we should keep some space for the bezier handles)
        timeline_width_pixels = (int)(timeline_width_pixels * 0.95f);

        // The technique is to reuse the _get_zoom_scale function directly to be sure that the auto_fit is always calculated
        // the same way as the zoom slider. It's a little bit more calculation then doing the inverse of get_zoom_scale but
        // it's really easier to understand and should always be accurate.
        float new_zoom = (float)zoom.MaxValue;
        while (true) {
            double test_zoom_scale = GetZoomScale(new_zoom);

            if (anim_length * test_zoom_scale <= timeline_width_pixels) {
                // It fits...
                break;
            }

            new_zoom -= (float)zoom.Step;

            if (new_zoom <= zoom.MinValue) {
                new_zoom = (float)zoom.MinValue;
                break;
            }
        }

        // Horizontal scroll to get_min which should include keyframes that are before the animation start.
        hscroll.Value = hscroll.MinValue;
        // Set the zoom value... the signal value_changed will be emitted and the timeline will be refreshed correctly!
        zoom.Value = new_zoom;
        // The new zoom value must be applied correctly so the scrollbar are updated before we move the scrollbar to
        // the beginning of the animation, hence the call deferred.
        // callable_mp(this, &AnimationTimelineEdit._scroll_to_start).call_deferred();
        CallDeferred(MethodName._ScrollToStart);
    }

    public void UpdatePlayPosition() {
        play_position.QueueRedraw();
    }

    public void UpdateValues() {
        if (animation is null || editing) {
            return;
        }

        editing = true;
        if (use_fps && animation.Step > 0.0) {
            length.Value = animation.Length / animation.Step;
            length.Step = FPS_DECIMAL;
            length.TooltipText = Tr("Animation length (frames)");
            time_icon.TooltipText = Tr("Animation length (frames)");
            // if (track_edit) {
            // 	track_edit.editor._update_key_edit();
            // 	track_edit.editor.marker_edit._update_key_edit();
            // }
        } else {
            length.Value = animation.Length;
            length.Step = SECOND_DECIMAL;
            length.TooltipText = Tr("Animation length (seconds)");
            time_icon.TooltipText = Tr("Animation length (seconds)");
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

    public void SetHScroll(HScrollBar p_hscroll) {
        hscroll = p_hscroll;
    }

    public virtual CursorShape GetCursorShape(Vector2 p_pos) {
        if (dragging_hsize || hsize_rect.HasPoint(p_pos)) {
            // Indicate that the track name column's width can be adjusted
            return CursorShape.Hsize;
        } else {
            return GetDefaultCursorShape();
        }
    }
    private void _ZoomChanged(double _) {
        double zoom_pivot = 0; // Point on timeline to stay fixed.
        double zoom_pivot_delta = 0; // Delta seconds from left-most point on timeline to zoom pivot.

        int timeline_width_pixels = (int)Size.X - ButtonsWidth - NameLimit;
        double timeline_width_seconds = timeline_width_pixels / last_zoom_scale; // Length (in seconds) of visible part of timeline before zoom.
        double updated_timeline_width_seconds = timeline_width_pixels / ZoomScale; // Length after zoom.
        double updated_timeline_half_width = updated_timeline_width_seconds / 2.0;
        bool zooming = updated_timeline_width_seconds < timeline_width_seconds;

        double timeline_left = Value;
        double timeline_right = timeline_left + timeline_width_seconds;
        double timeline_center = timeline_left + timeline_width_seconds / 2.0;

        if (zoom_callback_occured) { // Zooming with scroll wheel will focus on the position of the mouse.
            double zoom_scroll_origin_norm = (zoom_scroll_origin.X - NameLimit) / timeline_width_pixels;
            zoom_scroll_origin_norm = Mathf.Max(zoom_scroll_origin_norm, 0);
            zoom_pivot = timeline_left + timeline_width_seconds * zoom_scroll_origin_norm;
            zoom_pivot_delta = updated_timeline_width_seconds * zoom_scroll_origin_norm;
            zoom_callback_occured = false;
        } else { // Zooming with slider will depend on the current play position.
                 // If the play position is not in range, or exactly in the center, zoom in on the center.
            if (PlayPosition < timeline_left || PlayPosition > timeline_left + timeline_width_seconds || PlayPosition == timeline_center) {
                zoom_pivot = timeline_center;
                zoom_pivot_delta = updated_timeline_half_width;
            }
            // Zoom from right if play position is right of center,
            // and shrink from right if play position is left of center.
            else if ((PlayPosition > timeline_center) == zooming) {
                // If play position crosses to other side of center, center it.
                bool center_passed = (PlayPosition < timeline_right - updated_timeline_half_width) == zooming;
                zoom_pivot = center_passed ? PlayPosition : timeline_right;
                double center_offset = double.Epsilon * (zooming ? 1 : -1); // Small offset to prevent crossover.
                zoom_pivot_delta = center_passed ? updated_timeline_half_width + center_offset : updated_timeline_width_seconds;
            }
            // Zoom from left if play position is left of center,
            // and shrink from left if play position is right of center.
            else if ((PlayPosition <= timeline_center) == zooming) {
                // If play position crosses to other side of center, center it.
                bool center_passed = (PlayPosition > timeline_left + updated_timeline_half_width) == zooming;
                zoom_pivot = center_passed ? PlayPosition : timeline_left;
                double center_offset = double.Epsilon * (zooming ? -1 : 1); // Small offset to prevent crossover.
                zoom_pivot_delta = center_passed ? updated_timeline_half_width + center_offset : 0;
            }
        }

        double hscroll_pos = zoom_pivot - zoom_pivot_delta;
        hscroll_pos = Mathf.Clamp(hscroll_pos, hscroll.MinValue, hscroll.MaxValue);

        hscroll.Value = hscroll_pos;
        hscroll_on_zoom_buffer = hscroll_pos; // In case of page update.
        last_zoom_scale = ZoomScale;

        QueueRedraw();
        play_position.QueueRedraw();
        EmitSignal(SignalName.ZoomChanged);
    }
    private void _AnimLengthChanged(double p_new_len) {
        if (editing) {
            return;
        }

        p_new_len = Mathf.Max(SECOND_DECIMAL, p_new_len);
        if (use_fps && animation.Step > 0) {
            p_new_len *= animation.Step;
        }

        editing = true;
        // EditorUndoRedoManager *undo_redo = EditorUndoRedoManager.get_singleton();
        // undo_redo.create_action(Tr("Change Animation Length"), UndoRedo.MERGE_ENDS);
        // undo_redo.add_do_method(animation.ptr(), "set_length", p_new_len);
        // undo_redo.add_undo_method(animation.ptr(), "set_length", animation.Length);
        // undo_redo.commit_action();
        editing = false;
        QueueRedraw();

        EmitSignal(SignalName.LengthChanged, p_new_len);
    }
    private void _AnimLoopPressed() {
        if (!read_only) {
            // EditorUndoRedoManager *undo_redo = EditorUndoRedoManager.get_singleton();
            // undo_redo.create_action(Tr("Change Animation Loop"));
            // switch (animation.get_loop_mode()) {
            // 	case Animation.LOOP_NONE: {
            // 		undo_redo.add_do_method(animation.ptr(), "set_loop_mode", Animation.LOOP_LINEAR);
            // 	} break;
            // 	case Animation.LOOP_LINEAR: {
            // 		undo_redo.add_do_method(animation.ptr(), "set_loop_mode", Animation.LOOP_PINGPONG);
            // 	} break;
            // 	case Animation.LOOP_PINGPONG: {
            // 		undo_redo.add_do_method(animation.ptr(), "set_loop_mode", Animation.LOOP_NONE);
            // 	} break;
            // 	default:
            // 		break;
            // }
            // undo_redo.add_do_method(this, "update_values");
            // undo_redo.add_undo_method(animation.ptr(), "set_loop_mode", animation.get_loop_mode());
            // undo_redo.add_undo_method(this, "update_values");
            // undo_redo.commit_action();
        } else {
            string base_path = animation.ResourcePath;
            if (FileAccess.FileExists(base_path + ".import")) {
                // EditorNode.get_singleton().Show_warning(Tr("Can't change loop mode on animation instanced from imported scene."));
                OS.Alert(Tr("Can't change loop mode on animation instanced from imported scene."));
            } else {
                // EditorNode.get_singleton().Show_warning(Tr("Can't change loop mode on animation embedded in another scene."));
                OS.Alert(Tr("Can't change loop mode on animation embedded in another scene."));
            }
            UpdateValues();
        }
    }

    private void _PlayPositionDraw() {
        if (animation is null || play_position_pos < 0) {
            return;
        }

        float scale = ZoomScale;
        int h = (int)play_position.Size.Y;

        int px = (int)((-Value + play_position_pos) * scale + NameLimit);

        if (px >= NameLimit && px < (play_position.Size.X - ButtonsWidth)) {
            Color color = GetThemeColor("accent_color", "Editor");
            play_position.DrawLine(new(px, 0), new(px, h), color, Mathf.Round(2 * EDSCALE));
            play_position.DrawTexture(
                    GetThemeIcon("TimelineIndicator"),
                    new(px - GetThemeIcon("TimelineIndicator").GetWidth() * 0.5f, 0),
                    color);
        }
    }
    private void _PanCallback(Vector2 p_scroll_vec, InputEvent p_event) {
        Value -= p_scroll_vec.X / ZoomScale;
    }
    private void _ZoomCallback(float p_zoom_factor, Vector2 p_origin, InputEvent p_event) {
        double current_zoom_value = Zoom.Value;
        zoom_scroll_origin = p_origin;
        zoom_callback_occured = true;
        Zoom.Value = Math.Max(0.01, current_zoom_value - (1.0 - p_zoom_factor));
    }

    public override void _GuiInput (InputEvent p_event) {
        if (p_event is null)
            throw new NullReferenceException($"{nameof(p_event)} is null");

        if (panner.GuiInput(p_event)) {
            AcceptEvent();
            return;
        }

        if (p_event is InputEventMouseButton mb) {
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

            if (mb.Pressed && mb.ButtonIndex == MouseButton.Left && hsize_rect.HasPoint(mb.Position)) {
                dragging_hsize = true;
                dragging_hsize_from = mb.Position.X;
                dragging_hsize_at = name_limit;
            }

            if (!mb.Pressed && mb.ButtonIndex == MouseButton.Left && dragging_hsize) {
                dragging_hsize = false;
            }
            if (mb.Position.X > NameLimit && mb.Position.X < (Size.X - ButtonsWidth)) {
                if (!panner.Panning && mb.ButtonIndex == MouseButton.Left) {
                    int x = (int)mb.Position.X - NameLimit;

                    float ofs = x / ZoomScale + (float)Value;
                    EmitSignal(SignalName.TimelineChanged, ofs, mb.AltPressed);
                    dragging_timeline = true;
                }
            }

            if (dragging_timeline && mb.ButtonIndex == MouseButton.Left && !mb.Pressed) {
                dragging_timeline = false;
            }
        }

        if (p_event is InputEventMouseMotion mm) {
            if (dragging_hsize) {
                int ofs = (int)(mm.Position.X - dragging_hsize_from);
                name_limit = (int)dragging_hsize_at + ofs;
                // Make sure name_limit is clamped to the range that UI allows.
                name_limit = NameLimit;
                QueueRedraw();
                EmitSignal(SignalName.NameLimitChanged);
                play_position.QueueRedraw();
            }
            if (dragging_timeline) {
                int x = (int)mm.Position.X - NameLimit;
                float ofs = x / ZoomScale + (float)Value;
                EmitSignal(SignalName.TimelineChanged, ofs, mm.AltPressed);
            }
        }
    }
    private void _TrackAdded(long p_track) { }

    private float GetZoomScale(double p_zoom_value) {
        float zv = (float)(zoom.MaxValue - p_zoom_value);
        if (zv < 1) {
            zv = 1f - zv;
            return Mathf.Pow(1.0f + zv, 8.0f) * 100;
        } else {
            return 1f / Mathf.Pow(zv, 8.0f) * 100;
        }
    }
    private void _ScrollToStart() {
        // Horizontal scroll to get_min which should include keyframes that are before the animation start.
        hscroll.Value = hscroll.MinValue;
    }

    public override void _Notification(int p_what) {
        switch ((long)p_what) {
            case NotificationEnterTree:
            case NotificationThemeChanged: {
                add_track.Icon = GetThemeIcon("Add");
                loop.Icon = GetThemeIcon("Loop");
                time_icon.Texture = GetThemeIcon("Time");

                add_track.GetPopup().Clear();
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyValue"), Tr("Property Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyXPosition"), Tr("3D Position Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyXRotation"), Tr("3D Rotation Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyXScale"), Tr("3D Scale Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyBlendShape"), Tr("Blend Shape Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyCall"), Tr("Call Method Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyBezier"), Tr("Bezier Curve Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyAudio"), Tr("Audio Playback Track..."));
                add_track.GetPopup().AddIconItem(GetThemeIcon("KeyAnimation"), Tr("Animation Playback Track..."));
            }
            break;

            // case EditorSettings.NotificationEditorSettingsChanged: {
            //     if (EditorSettings.get_singleton().check_changed_settings_in_group("editors/panning")) {
            //         panner.setup((ViewPanner.ControlScheme)EDITOR_GET("editors/panning/animation_editors_panning_scheme").operator int(), ED_GET_SHORTCUT("canvas_item_editor/pan_view"), bool(EDITOR_GET("editors/panning/simple_panning")));
            //     }
            // }
            // break;

            case NotificationResized: {
                len_hb.Position = new Vector2(Size.X - ButtonsWidth, 0);
                len_hb.Size = new Vector2(ButtonsWidth, Size.Y);
            }
            break;

            case NotificationDraw: {
                int key_range = (int)Size.X - ButtonsWidth - NameLimit;

                if (animation is null) {
                    return;
                }

                Font font = GetThemeFont("font", "Label");
                int font_size = GetThemeFontSize("font_size", "Label");

                StyleBox stylebox_time_unavailable = GetThemeStylebox("time_unavailable", "AnimationTimelineEdit");
                StyleBox stylebox_time_available = GetThemeStylebox("time_available", "AnimationTimelineEdit");

                Color v_line_primary_color = GetThemeColor("v_line_primary_color", "AnimationTimelineEdit");
                Color v_line_secondary_color = GetThemeColor("v_line_secondary_color", "AnimationTimelineEdit");
                Color h_line_color = GetThemeColor("h_line_color", "AnimationTimelineEdit");
                Color font_primary_color = GetThemeColor("font_primary_color", "AnimationTimelineEdit");
                Color font_secondary_color = GetThemeColor("font_secondary_color", "AnimationTimelineEdit");

                int v_line_primary_margin = GetThemeConstant("v_line_primary_margin", "AnimationTimelineEdit");
                int v_line_secondary_margin = GetThemeConstant("v_line_secondary_margin", "AnimationTimelineEdit");
                int v_line_primary_width = GetThemeConstant("v_line_primary_width", "AnimationTimelineEdit");
                int v_line_secondary_width = GetThemeConstant("v_line_secondary_width", "AnimationTimelineEdit");
                int text_primary_margin = GetThemeConstant("text_primary_margin", "AnimationTimelineEdit");
                int text_secondary_margin = GetThemeConstant("text_secondary_margin", "AnimationTimelineEdit");

                int zoomw = key_range;
                float scale = ZoomScale;
                int h = (int)Size.Y;

                float l = animation.Length;
                if (l <= 0) {
                    l = (float)SECOND_DECIMAL; // Avoid crashor.
                }

                Texture2D hsize_icon = GetThemeIcon("Hsize");
                hsize_rect = new Rect2(NameLimit - hsize_icon.GetWidth() - 8 * EDSCALE, (Size.Y - hsize_icon.GetHeight()) / 2, hsize_icon.GetWidth(), hsize_icon.GetHeight());
                DrawTexture(hsize_icon, hsize_rect.Position);

                {
                    float time_min = 0;
                    float time_max = animation.Length;
                    for (int i = 0; i < animation.GetTrackCount(); i++) {
                        if (animation.TrackGetKeyCount(i) > 0) {
                            float beg = (float)animation.TrackGetKeyTime(i, 0);

                            if (beg < time_min) {
                                time_min = beg;
                            }

                            float end = (float)animation.TrackGetKeyTime(i, animation.TrackGetKeyCount(i) - 1);

                            if (end > time_max) {
                                time_max = end;
                            }
                        }
                    }

                    string[] markers = animation.GetMarkerNames();
                    if (markers.Length > 0) {
                        float min_marker = (float)animation.GetMarkerTime(markers[0]);
                        float max_marker = (float)animation.GetMarkerTime(markers[^1]);
                        if (min_marker < time_min) {
                            time_min = min_marker;
                        }
                        if (max_marker > time_max) {
                            time_max = max_marker;
                        }
                    }

                    float extra = zoomw / scale * 0.5f;

                    time_max += extra;
                    MinValue = time_min;
                    MaxValue = time_max;

                    if (zoomw / scale < (time_max - time_min)) {
                        hscroll.Show();

                    } else {
                        hscroll.Hide();
                    }
                }

                Page = zoomw / scale;

                if (hscroll.Visible && hscroll_on_zoom_buffer >= 0) {
                    hscroll.Value = hscroll_on_zoom_buffer;
                    hscroll_on_zoom_buffer = -1.0;
                }

                int end_px = (int)((l - Value) * scale);
                int begin_px = (int)(-Value * scale);

                {
                    DrawStyleBox(stylebox_time_unavailable, new(new(NameLimit, 0), new(zoomw - 1, h)));

                    if (begin_px < zoomw && end_px > 0) {
                        if (begin_px < 0) {
                            begin_px = 0;
                        }
                        if (end_px > zoomw) {
                            end_px = zoomw;
                        }

                        DrawStyleBox(stylebox_time_available, new(new(NameLimit + begin_px, 0), new(end_px - begin_px, h)));
                    }
                }

                int dec = 1;
                int step = 1;
                int decimals = 2;
                bool step_found = false;

                float period_width = font.GetCharSize('.', font_size).X;
                float max_digit_width = font.GetCharSize('0', font_size).X;
                for (int i = 1; i <= 9; i++) {
                    float digit_width = font.GetCharSize('0' + i, font_size).X;
                    max_digit_width = Mathf.Max(digit_width, max_digit_width);
                }
                int max_sc = Mathf.CeilToInt(zoomw / scale);
                int max_sc_width = max_sc.ToString().Length * Mathf.CeilToInt(max_digit_width);

                int min_margin = Mathf.Max(text_secondary_margin, text_primary_margin);

                while (!step_found) {
                    int min = max_sc_width;
                    if (decimals > 0) {
                        min += Mathf.CeilToInt(period_width + max_digit_width * decimals);
                    }

                    min += min_margin * 2;

                    int[] _multp = [ 1, 2, 5 ];
                    for (int i = 0; i < 3; i++) {
                        step = _multp[i] * dec;
                        if (step * scale / SC_ADJ > min) {
                            step_found = true;
                            break;
                        }
                    }
                    if (step_found) {
                        break;
                    }
                    dec *= 10;
                    decimals--;
                    if (decimals < 0) {
                        decimals = 0;
                    }
                }

                if (use_fps) {
                    float step_size = animation.Step;
                    if (step_size > 0) {
                        int prev_frame_ofs = -10000000;

                        for (int i = 0; i < zoomw; i++) {
                            float pos = (float)(Value + (double)i / scale);
                            float prev = (float)(Value + ((double)i - 1.0) / scale);

                            int frame = (int)(pos / step_size);
                            int prev_frame = (int)(prev / step_size);

                            bool sub = Mathf.Floor(prev) == Mathf.Floor(pos);

                            if (frame != prev_frame && i >= prev_frame_ofs) {
                                int line_margin = sub ? v_line_secondary_margin : v_line_primary_margin;
                                int line_width = sub ? v_line_secondary_width : v_line_primary_width;
                                Color line_color = sub ? v_line_secondary_color : v_line_primary_color;

                                DrawLine(new(NameLimit + i, 0 + line_margin), new(NameLimit + i, h - line_margin), line_color, line_width);

                                int text_margin = sub ? text_secondary_margin : text_primary_margin;

                                DrawString(font, new Vector2(NameLimit + i + text_margin, (h - font.GetHeight(font_size)) / 2 + font.GetAscent(font_size)).Floor(), frame.ToString(), HorizontalAlignment.Left, zoomw - i, font_size, sub ? font_secondary_color : font_primary_color);

                                prev_frame_ofs = i + (int)font.GetStringSize(frame.ToString(), HorizontalAlignment.Left, -1, font_size).X + text_margin;
                            }
                        }
                    }

                } else {
                    for (int i = 0; i < zoomw; i++) {
                        float pos = (float)(Value + (double)i / scale);
                        float prev = (float)(Value + ((double)i - 1.0) / scale);

                        int sc = Mathf.FloorToInt(pos * SC_ADJ);
                        int prev_sc = Mathf.FloorToInt(prev * SC_ADJ);

                        if ((sc / step) != (prev_sc / step) || (prev_sc < 0 && sc >= 0)) {
                            int scd = sc < 0 ? prev_sc : sc;
                            bool sub = ((scd - (scd % step)) % (dec * 10)) != 0;

                            int line_margin = sub ? v_line_secondary_margin : v_line_primary_margin;
                            int line_width = sub ? v_line_secondary_width : v_line_primary_width;
                            Color line_color = sub ? v_line_secondary_color : v_line_primary_color;

                            DrawLine(new(NameLimit + i, 0 + line_margin), new(NameLimit + i, h - line_margin), line_color, line_width);

                            int text_margin = sub ? text_secondary_margin : text_primary_margin;

                            DrawString(font, new Vector2(NameLimit + i + text_margin, (h - font.GetHeight(font_size)) / 2 + font.GetAscent(font_size)).Floor(), ((scd - (scd % step)) / (double)SC_ADJ).ToString($"F{decimals}", System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, zoomw - i, font_size, sub ? font_secondary_color : font_primary_color);
                        }
                    }
                }

                DrawLine(new(0, Size.Y), Size, h_line_color, Mathf.Round(EDSCALE));
                UpdateValues();
            }
            break;
        }
    }
}