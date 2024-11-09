/**************************************************************************/
/*  animation_track_editor.cpp                                            */
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

using System.Globalization;
using System.Linq;
using Godot;

namespace PCE.Editor;

[GlobalClass]
public partial class Timeline : Range {
    private readonly ViewPanner panner;
    private readonly Range zoom;
    public float ZoomScale => GetZoomScale(zoom.Value);
    private Animation animation;
    public Animation Animation {
        get => animation;
        set {
            animation = value;
            QueueRedraw();
        }
    }

    public Timeline() {
        MinValue = 0;
        Step = 0;
        AllowGreater = true;
        panner = new();
        panner.SetCallbacks(OnPan, OnZoom);
        panner.PanAxis = ViewPanner.PanAxisEnum.Horizontal;
        panner.SetScrollZoomFactor(1.02f);
        zoom = new() {
            Value = 1, // 0.5 steps by default
            MaxValue = 2, // max 200 steps
            Step = 0 // it needs to be smooth
        };
    }

    public override void _GuiInput(InputEvent @event) {
        if (panner.GuiInput(@event)) {
            AcceptEvent();
            QueueRedraw();
        }
    }

    public override void _Draw() {
        // Vector2 pos = new((float)Value, Size.Y / 2f);
        // float r = 5 * (float)Zoom.Value;
        // DrawCircle(pos, r, Colors.Red);
        if (animation is null)
            return;

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

        int keyRange = (int)Size.X;

        int zoomw = keyRange;
        float scale = ZoomScale;
        int h = (int)Size.Y;

        float l = animation.Length;
        if (l <= 0) {
            l = 0.0001f; // SECOND_DECIAML // Avoid crashor.
        }

        // update min & max value
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

            float extra = (zoomw / scale) * 0.5f;

            time_max += extra;
            MinValue = time_min;
            MaxValue = time_max;

            if (zoomw / scale < (time_max - time_min)) {
                // hscroll.show();

            } else {
                // hscroll.hide();
            }
        }

        Page = zoomw / scale;

        // if (hscroll.is_visible() && hscroll_on_zoom_buffer >= 0) {
        //     hscroll.set_value(hscroll_on_zoom_buffer);
        //     hscroll_on_zoom_buffer = -1.0;
        // }

        int end_px = (int)((l - Value) * scale);
        int begin_px = (int)(-Value * scale);

        // outline
        {
            DrawStyleBox(stylebox_time_unavailable, new(new(0, 0), new(zoomw - 1, h)));

            if (begin_px < zoomw && end_px > 0) {
                if (begin_px < 0) {
                    begin_px = 0;
                }
                if (end_px > zoomw) {
                    end_px = zoomw;
                }

                DrawStyleBox(stylebox_time_available, new(new(0 + begin_px, 0), new(end_px - begin_px, h)));
            }
        }

        // guides
        {
            int dec = 1;
            int step = 1;
            int decimals = 2;
            bool step_found = false;

            float period_width = font.GetCharSize('.', font_size).X;
            float max_digit_width = "0123456789".Max(it => font.GetCharSize(it, font_size).X);

            int max_sc = Mathf.CeilToInt(zoomw / scale);
            int max_sc_width =max_sc.ToString().Length * Mathf.CeilToInt(max_digit_width);

            int min_margin = Mathf.Max(text_secondary_margin, text_primary_margin);

            while (!step_found) {
                int min = max_sc_width;
                if (decimals > 0) {
                    min += Mathf.CeilToInt(period_width + max_digit_width * decimals);
                }

                min += min_margin * 2;

                int[] _multp = [1, 2, 5];
                for (int i = 0; i < 3; i++) {
                    step = (_multp[i] * dec);
                    if (step * scale / 100f > min) {
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

            for (int i = 0; i < zoomw; i++) {
                float pos = (float)(Value + i / scale);
                float prev = (float)(Value + (i - 1.0) / scale);

                int sc = Mathf.FloorToInt(pos * 100f);
                int prev_sc = Mathf.FloorToInt(prev * 100f);

                if ((sc / step) != (prev_sc / step) || (prev_sc < 0 && sc >= 0)) {
                    int scd = sc < 0 ? prev_sc : sc;
                    bool sub = ((scd - (scd % step)) % (dec * 10)) != 0;

                    int line_margin = sub ? v_line_secondary_margin : v_line_primary_margin;
                    int line_width = sub ? v_line_secondary_width : v_line_primary_width;
                    Color line_color = sub ? v_line_secondary_color : v_line_primary_color;

                    DrawLine(new(i, line_margin), new(i, h - line_margin), line_color, line_width);

                    int text_margin = sub ? text_secondary_margin : text_primary_margin;

                    DrawString(font, new Vector2(i + text_margin, (h - font.GetHeight(font_size)) / 2 + font.GetAscent(font_size)).Floor(), ((scd - (scd % step)) / 100d).ToString($"F{decimals}", CultureInfo.InvariantCulture), HorizontalAlignment.Left, zoomw - i, font_size, sub ? font_secondary_color : font_primary_color);
                }
            }
        }
    }

    private void OnPan(Vector2 scroll, InputEvent @event) {
        Value -= scroll.X / ZoomScale;
    }

    private void OnZoom(float zoomFactor, Vector2 origin, InputEvent @event) {
        zoom.Value = Mathf.Max(0.01, zoom.Value - (1.0 - zoomFactor));
    }

    private float GetZoomScale(double zoomValue) {
        float zv = (float)(zoom.MaxValue - zoomValue);
        if (zv < 1) {
            zv = 1f - zv;
            return Mathf.Pow(1.0f + zv, 8.0f) * 100;
        } else {
            return 1f / Mathf.Pow(zv, 8.0f) * 100;
        }
    }
}