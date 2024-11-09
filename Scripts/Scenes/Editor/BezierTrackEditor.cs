/**************************************************************************/
/*  animation_bezier_editor.cpp, animation_bezier_editor.h                */
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

using System.Collections.Generic;
using Godot;

namespace PCE.Editor;

[GlobalClass]
public partial class BezierTrackEditor : Control {
    private const float EDSCALE = 1f;
    private readonly ViewPanner panner;
    private float track_v_scroll = 0;
    private float track_v_scroll_max = 0;
    private float timeline_v_scroll = 0;
    private float timeline_v_zoom = 1;

    private Timeline timeline;
    private Animation animation;

    public BezierTrackEditor() {
        panner = new();
        panner.Setup(ViewPanner.ControlSchemeEnum.ScrollPans, new Shortcut() { Events = [new InputEventKey() { Keycode = Key.Space }] }, false);
        panner.SetCallbacks(OnPan, OnZoom);
        animation = ResourceLoader.Load<Animation>("res://test.tres");
    }

    public override void _Ready() {
        timeline = GetNode<Timeline>("../TimeMarkings/Timeline");
        timeline.Animation = animation;
    }

    public override void _GuiInput(InputEvent @event) {
        if (panner.GuiInput(@event)) {
            AcceptEvent();
            return;
        }
    }

    public override void _Draw() {
        // Vector2 p = new(track_v_scroll, 0);
        // float r = 50;
        // DrawCircle(p, r, Colors.Red);
        // p = new((float)timeline.Value, timeline_v_scroll);
        // DrawCircle(p, r, Colors.Blue);
        if (animation is null)
            return;

        int limit = 0;/* timeline.get_name_limit(); */

        Font font = GetThemeFont(("font"), ("Label"));
        int font_size = GetThemeFontSize(("font_size"), ("Label"));
        Color color = GetThemeColor(("font_color"), ("Label"));

        Color h_line_color = GetThemeColor(("h_line_color"), ("AnimationBezierTrackEdit"));
        Color v_line_color = GetThemeColor(("v_line_color"), ("AnimationBezierTrackEdit"));
        Color focus_color = GetThemeColor(("focus_color"), ("AnimationBezierTrackEdit"));
        Color track_focus_color = GetThemeColor(("track_focus_color"), ("AnimationBezierTrackEdit"));

        int h_separation = GetThemeConstant(("h_separation"), ("AnimationBezierTrackEdit"));
        int v_separation = GetThemeConstant(("h_separation"), ("AnimationBezierTrackEdit"));

        if (HasFocus()) {
            DrawRect(new(new(), Size), focus_color, false, Mathf.Round(EDSCALE));
        }

        DrawLine(new(limit, 0), new(limit, Size.Y), v_line_color, Mathf.Round(EDSCALE));

        int right_limit = (int)Size.X;

        track_v_scroll_max = v_separation;

        int vofs = v_separation + (int)track_v_scroll;
        int margin = 0;

        // Guides.
        {
            float min_left_scale = font.GetHeight(font_size) + v_separation;

            float scale = (min_left_scale * 2) * timeline_v_zoom;
            float step = Mathf.Pow(10f, Mathf.Round(Mathf.Log(scale / 5f) / Mathf.Log(10f))) * 5f;
            scale = Mathf.Snapped(scale, step);

            while (scale / timeline_v_zoom < min_left_scale * 2) {
                scale += step;
            }

            bool first = true;
            int prev_iv = 0;
            for (int i = (int)font.GetHeight(font_size); i < Size.Y; i++) {
                float ofs = Size.Y / 2f - i;
                ofs *= timeline_v_zoom;
                ofs += timeline_v_scroll;

                int iv = (int)(ofs / scale);
                if (ofs < 0) {
                    iv -= 1;
                }
                if (!first && iv != prev_iv) {
                    Color lc = h_line_color;
                    lc.A *= 0.5f;
                    DrawLine(new(limit, i), new(right_limit, i), lc, Mathf.Round(EDSCALE));
                    Color c = color;
                    c.A *= 0.5f;
                    DrawString(font, new(limit + 8, i - 2), TextServerManager.GetPrimaryInterface().FormatNumber(Mathf.Snapped((iv + 1) * scale, step).ToString(System.Globalization.CultureInfo.InvariantCulture)), HorizontalAlignment.Left, -1, font_size, c);
                }

                first = false;
                prev_iv = iv;
            }
        }

        // Draw other curves.
        {
            //     float scale = timeline.get_zoom_scale();
            //     Ref<Texture2D> point = get_editor_theme_icon(SNAME("KeyValue"));
            //     for (const KeyValue<int, Color> &E : subtrack_colors) {
            //         if (hidden_tracks.has(E.key)) {
            //             continue;
            //         }
            //         _draw_track(E.key, E.value);

            //         for (int i = 0; i < animation.TrackGetKeyCount(E.key); i++) {
            //             float offset = animation.TrackGetKeyTime(E.key, i);
            //             float value = animation.bezier_track_get_key_value(E.key, i);

            //             Vector2 pos((offset -timeline.get_value()) *scale + limit, _bezier_h_to_pixel(value));

            //         if (pos.x >= limit && pos.x <= right_limit) {
            //             draw_texture(point, pos - point.get_size() / 2.0, E.value);
            //         }
            //     }
            // }

            // if (track_count > 0 && !hidden_tracks.has(selected_track)) {
            //     // Draw edited curve.
            //     _draw_track(selected_track, selected_track_color);
            // }
            DrawTrack(1, Colors.NavyBlue);
        }
    }

    private void DrawTrack(int track, Color color) {
        float scale = timeline.ZoomScale;

        int limit = 0; /* timeline.get_name_limit(); */
        int right_limit = (int)Size.X;

        // Selection may have altered the order of keys.
        SortedList<double, int> key_order = [];

        for (int i = 0; i < animation.TrackGetKeyCount(track); i++) {
            double ofs = animation.TrackGetKeyTime(track, i);
            // if (moving_selection && selection.has(IntPair(track, i))) {
            //     ofs += moving_selection_offset.x;
            // }

            key_order[ofs] = i;
        }

        for (int e = 0; e < key_order.Count - 1; e++) {
            int i = key_order.Values[e];

            int i_n = key_order.Values[e + 1];

            float offset = (float)animation.TrackGetKeyTime(track, i);
            float height = animation.BezierTrackGetKeyValue(track, i);
            Vector2 out_handle = animation.BezierTrackGetKeyOutHandle(track, i);
            // if (track == moving_handle_track && (moving_handle == -1 || moving_handle == 1) && moving_handle_key == i) {
            //     out_handle = moving_handle_right;
            // }

            // if (moving_selection && selection.has(IntPair(track, i))) {
            //     offset += moving_selection_offset.x;
            //     height += moving_selection_offset.y;
            // }

            out_handle += new Vector2(offset, height);

            float offset_n = (float)animation.TrackGetKeyTime(track, i_n);
            float height_n = animation.BezierTrackGetKeyValue(track, i_n);
            Vector2 in_handle = animation.BezierTrackGetKeyInHandle(track, i_n);
            // if (track == moving_handle_track && (moving_handle == -1 || moving_handle == 1) && moving_handle_key == i_n) {
            //     in_handle = moving_handle_left;
            // }

            // if (moving_selection && selection.has(IntPair(track, i_n))) {
            //     offset_n += moving_selection_offset.x;
            //     height_n += moving_selection_offset.y;
            // }

            in_handle += new Vector2(offset_n, height_n);

            Vector2 start = new(offset, height);
            Vector2 end = new(offset_n, height_n);

            int from_x = (int)((offset - timeline.Value) * scale + limit);
            int point_start = from_x;
            int to_x = (int)((offset_n - timeline.Value) * scale + limit);
            int point_end = to_x;

            if (from_x > right_limit) { // Not visible.
                continue;
            }

            if (to_x < limit) { // Not visible.
                continue;
            }

            from_x = Mathf.Max(from_x, limit);
            to_x = Mathf.Min(to_x, right_limit);

            List<Vector2> lines = [];

            Vector2 prev_pos = new();

            for (int j = from_x; j <= to_x; j++) {
                float t = (float)((j - limit) / scale + timeline.Value);

                float h;

                if (j == point_end) {
                    h = end.Y; // Make sure it always connects.
                } else if (j == point_start) {
                    h = start.Y; // Make sure it always connects.
                } else { // Custom interpolation, used because it needs to show paths affected by moving the selection or handles.
                    int iterations = 10;
                    float low = 0;
                    float high = 1;

                    // Narrow high and low as much as possible.
                    for (int k = 0; k < iterations; k++) {
                        float middle = (low + high) / 2f;

                        Vector2 interp = start.BezierInterpolate(out_handle, in_handle, end, middle);

                        if (interp.X < t) {
                            low = middle;
                        } else {
                            high = middle;
                        }
                    }

                    // Interpolate the result.
                    Vector2 low_pos = start.BezierInterpolate(out_handle, in_handle, end, low);
                    Vector2 high_pos = start.BezierInterpolate(out_handle, in_handle, end, high);

                    float c = (t - low_pos.X) / (high_pos.X - low_pos.X);

                    h = low_pos.Lerp(high_pos, c).Y;
                }

                h = BezierHToPixel(h);

                Vector2 pos = new(j, h);

                if (j > from_x) {
                    lines.Add(prev_pos);
                    lines.Add(pos);
                }
                prev_pos = pos;
            }

            if (lines.Count >= 2) {
                DrawMultiline([.. lines], color, Mathf.Round(EDSCALE), true);
            }
        }
    }

    private float BezierHToPixel(float h) {
        h = (h - timeline_v_scroll) / timeline_v_zoom;
        h = (Size.Y / 2f) - h;
        return h;
    }

    private void OnPan(Vector2 scroll, InputEvent @event) {
        if (@event is InputEventMouseMotion mm) {
            if (mm.Position.X > 0) {
                timeline_v_scroll += scroll.Y * timeline_v_zoom;
                timeline_v_scroll = Mathf.Clamp(timeline_v_scroll, -100000, 100000);
                timeline.Value -= scroll.X / timeline.ZoomScale;
            } else {
                track_v_scroll += scroll.Y;
                if (track_v_scroll < -track_v_scroll_max) {
                    track_v_scroll = -track_v_scroll_max;
                } else if (track_v_scroll > 0) {
                    track_v_scroll = 0;
                }
            }
            QueueRedraw();
        }
    }

    private void OnZoom(float zoomFactor, Vector2 origin, InputEvent @event) {
        float vZoomOrig = timeline_v_zoom;
        if (@event is InputEventWithModifiers iewm && iewm.AltPressed) {
            // Alternate zoom (doesn't affect timeline).
            timeline_v_zoom = Mathf.Clamp(timeline_v_zoom * zoomFactor, 0.000001f, 100000f);
        } else {
            float zoom_factor = zoomFactor > 1.0 ? Timeline.ScrollZoomFactorIn : Timeline.ScrollZoomFactorOut;
            timeline.ZoomCallback(zoom_factor, origin, @event);
            timeline.QueueRedraw();
        }
        timeline_v_scroll += (origin.Y - Size.Y / 2.0f) * (timeline_v_zoom - vZoomOrig);
        QueueRedraw();
    }
}