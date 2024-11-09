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
    public const float ScrollZoomFactorIn = 1.02f; // Zoom factor per mouse scroll in the animation editor when zooming in. The closer to 1.0, the finer the control.
	public const float ScrollZoomFactorOut = 0.98f; // Zoom factor when zooming out. Similar to SCROLL_ZOOM_FACTOR_IN but less than 1.0.
    private const float ScAdj = 100f;
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
        panner.SetCallbacks(PanCallback, ZoomCallback);
        panner.PanAxis = ViewPanner.PanAxisEnum.Horizontal;
        panner.SetScrollZoomFactor(ScrollZoomFactorIn);
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
        int fontSize = GetThemeFontSize("font_size", "Label");

        StyleBox styleboxTimeUnavailable = GetThemeStylebox("time_unavailable", "AnimationTimelineEdit");
        StyleBox styleboxTimeAvailable = GetThemeStylebox("time_available", "AnimationTimelineEdit");

        Color vLinePrimaryColor = GetThemeColor("v_line_primary_color", "AnimationTimelineEdit");
        Color vLineSecondaryColor = GetThemeColor("v_line_secondary_color", "AnimationTimelineEdit");
        Color hLineColor = GetThemeColor("h_line_color", "AnimationTimelineEdit");
        Color FontPrimaryColor = GetThemeColor("font_primary_color", "AnimationTimelineEdit");
        Color FontSecondaryColor = GetThemeColor("font_secondary_color", "AnimationTimelineEdit");

        int vLinePrimaryMargin = GetThemeConstant("v_line_primary_margin", "AnimationTimelineEdit");
        int vLineSecondaryMargin = GetThemeConstant("v_line_secondary_margin", "AnimationTimelineEdit");
        int vLinePrimaryWidth = GetThemeConstant("v_line_primary_width", "AnimationTimelineEdit");
        int vLineSecondaryWidth = GetThemeConstant("v_line_secondary_width", "AnimationTimelineEdit");
        int textPrimaryMargin = GetThemeConstant("text_primary_margin", "AnimationTimelineEdit");
        int textSecondaryMargin = GetThemeConstant("text_secondary_margin", "AnimationTimelineEdit");

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

        int endPx = (int)((l - Value) * scale);
        int beginPx = (int)(-Value * scale);

        // outline
        {
            DrawStyleBox(styleboxTimeUnavailable, new(new(0, 0), new(zoomw - 1, h)));

            if (beginPx < zoomw && endPx > 0) {
                if (beginPx < 0) {
                    beginPx = 0;
                }
                if (endPx > zoomw) {
                    endPx = zoomw;
                }

                DrawStyleBox(styleboxTimeAvailable, new(new(0 + beginPx, 0), new(endPx - beginPx, h)));
            }
        }

        // guides
        {
            int dec = 1;
            int step = 1;
            int decimals = 2;
            bool stepFound = false;

            float periodWidth = font.GetCharSize('.', fontSize).X;
            float maxDigitWidth = "0123456789".Max(it => font.GetCharSize(it, fontSize).X);

            int maxSc = Mathf.CeilToInt(zoomw / scale);
            int maxScWidth = maxSc.ToString().Length * Mathf.CeilToInt(maxDigitWidth);

            int minMargin = Mathf.Max(textSecondaryMargin, textPrimaryMargin);

            while (!stepFound) {
                int min = maxScWidth;
                if (decimals > 0) {
                    min += Mathf.CeilToInt(periodWidth + maxDigitWidth * decimals);
                }

                min += minMargin * 2;

                int[] _multp = [1, 2, 5];
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

            for (int i = 0; i < zoomw; i++) {
                float pos = (float)(Value + i / scale);
                float prev = (float)(Value + (i - 1.0) / scale);

                int sc = Mathf.FloorToInt(pos * ScAdj);
                int prev_sc = Mathf.FloorToInt(prev * ScAdj);

                if ((sc / step) != (prev_sc / step) || (prev_sc < 0 && sc >= 0)) {
                    int scd = sc < 0 ? prev_sc : sc;
                    bool sub = ((scd - (scd % step)) % (dec * 10)) != 0;

                    int lineMargin = sub ? vLineSecondaryMargin : vLinePrimaryMargin;
                    int lineWidth = sub ? vLineSecondaryWidth : vLinePrimaryWidth;
                    Color lineColor = sub ? vLineSecondaryColor : vLinePrimaryColor;

                    DrawLine(new(i, lineMargin), new(i, h - lineMargin), lineColor, lineWidth);

                    int textMargin = sub ? textSecondaryMargin : textPrimaryMargin;

                    DrawString(font, new Vector2(i + textMargin, (h - font.GetHeight(fontSize)) / 2 + font.GetAscent(fontSize)).Floor(), ((scd - (scd % step)) / (double)ScAdj).ToString($"F{decimals}", CultureInfo.InvariantCulture), HorizontalAlignment.Left, zoomw - i, fontSize, sub ? FontSecondaryColor : FontPrimaryColor);
                }
            }
        }
    }

    private void PanCallback(Vector2 scroll, InputEvent @event) {
        Value -= scroll.X / ZoomScale;
    }

    public void ZoomCallback(float zoomFactor, Vector2 origin, InputEvent @event) {
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