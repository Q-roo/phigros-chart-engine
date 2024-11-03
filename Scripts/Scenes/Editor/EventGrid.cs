using System;
using System.Collections.Generic;
using Godot;
using PCE.Chart;

namespace PCE.Editor;

public enum HandleMode {
    Free,
    Linear,
    Balanced,
    Mirored
}

public struct BezierPoint : IComparable<BezierPoint> {
    public double time;
    public double value;
    public Vector2 inHandle;
    public Vector2 outHandle;
    public HandleMode handleMode;

    public readonly int CompareTo(BezierPoint other) {
        return time.CompareTo(other.time);
    }
}

public class BezierTrack {
    public readonly SortedList<double, BezierPoint> timeValues = [];
    public BezierTrack() {
        // test data
        // the rotation track
        Add(new() {
            time = 1.8333,
            value = -157,
            inHandle = new(-0.25f, 0),
            outHandle = new(0.25f, 0),
            handleMode = HandleMode.Free
        });
        Add(new() {
            time = 6.5333,
            value = 126.862,
            inHandle = new(0, -0.25f),
            outHandle = new(0, 0.25f),
            handleMode = HandleMode.Free
        });
        Add(new() {
            time = 10.3667,
            value = 123.5,
            inHandle = new(-0.25f, 0),
            outHandle = new(2.7f, -505),
            handleMode = HandleMode.Free
        });
        Add(new() {
            time = 14.0667,
            value = 220.5,
            inHandle = new(-6.333f, 231),
            outHandle = new(0.25f, 0),
            handleMode = HandleMode.Free
        });
        Add(new() {
            time = 15.1,
            value = 95.5,
            inHandle = new(-0.25f, 0),
            outHandle = new(0.25f, 0),
            handleMode = HandleMode.Free
        });
    }

    public void Add(BezierPoint point) {
        timeValues.Add(point.time, point);
    }
}

public partial class EventGrid : Panel {
    private ViewPanner panner;
    private AnimationTrackTimelineEdit timeline;

    float trackVScroll = 0;
    float trackVScrollMax = 0;

    float timelineVScroll = 0;
    float timelineVZoom = 0.00111f; // 1 is 100, 0.00348 is 1.5 and 0.00111 is 0.1

    public EventGrid() {

    }

    public override void _Ready() {
        panner = new();
        panner.SetCallbacks(PanCallback, ZoomCallback);
        panner.Setup(ViewPanner.ControlSchemeEnum.ScrollPans, new Shortcut() { Events = [new InputEventKey() { Keycode = Key.Space }] }, false);
        LayoutDirection = LayoutDirectionEnum.Rtl;
        timeline = GetNode<AnimationTrackTimelineEdit>("../TimeMarkings/Timeline");
        timeline.Zoom = new() { Value = 0.1, MinValue = 0, MaxValue = 1, Step = 0 };
        timeline.CustomMinimumSize = new(600, 40);
        HScrollBar bar = new();
        AddChild(bar);
        timeline.SetHscroll(bar);
        SetTimeline(timeline);
        // test
        AnimationPlayer player = GetNode<AnimationPlayer>("%Reference");
        Animation anim = player.GetAnimation("new_animation");
        timeline.SetAnimation(anim, false);
    }

    public void SetTimeline(AnimationTrackTimelineEdit timeline) {
        this.timeline = timeline;
        timeline.ZoomChanged += ZoomChanged;
        timeline.NameLimitChanged += ZoomChanged;
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is null)
            throw new NullReferenceException($"{nameof(@event)} is null");

        if (panner.GuiInput(@event)) {
            AcceptEvent();
            return;
        }
    }

    public override void _Draw() {
        float limit = timeline.NameLimit;
        float rightLimit = Size.X;
        float EDSCALE = 1;
        Color hLineColor = Colors.SlateGray;
        Color vLineColor = Colors.Green;
        Color color = Colors.White;
        float vSeparation = 1;
        Font font = GetThemeDefaultFont();
        int fontSize = GetThemeDefaultFontSize();
        float minLeftScale = font.GetHeight(fontSize) + vSeparation;

        if (HasFocus()) {
            DrawRect(new(new(), Size), Colors.DimGray, false);
        }

        DrawLine(new(limit, 0), new(limit, Size.Y), vLineColor, Mathf.Round(EDSCALE));

        // guides
        {
            float scale = minLeftScale * 2f * timelineVZoom;
            float step = Mathf.Pow(10f, Mathf.Round(Mathf.Log(scale / 5f) / Mathf.Log(10f))) * 5f;
            scale = Mathf.Snapped(scale, step);

            while (scale / timelineVZoom < minLeftScale * 2) {
                scale += step;
            }

            bool first = true;
            int prev_iv = 0;
            for (int i = (int)font.GetHeight(fontSize); i < Size.Y; i++) {
                float ofs = Size.Y / 2.0f - i;
                ofs *= timelineVZoom;
                ofs += timelineVScroll;

                int iv = (int)(ofs / scale);
                if (ofs < 0) {
                    iv -= 1;
                }
                if (!first && iv != prev_iv) {
                    Color lc = hLineColor;
                    lc.A *= 0.5f;
                    DrawLine(new(limit, i), new(rightLimit, i), lc, Mathf.Round(EDSCALE));
                    Color c = color;
                    c.A *= 0.5f;
                    // use 3 decimal accuracy because the numbers would look hideous otherwise
                    DrawString(font, new(limit + 8, i - 2), Mathf.Snapped((iv + 1) * scale, step).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), HorizontalAlignment.Left, -1, fontSize, c);
                }

                first = false;
                prev_iv = iv;
            }

            step = ChartGlobals.DistanceBetweenBeats; // timeline can take care of this's scaling
            int visibleBeats = Mathf.CeilToInt(Size.X / step);

            for (int i = 0; i < visibleBeats; i++) {
                DrawLine(new(limit + i * step + (float)timeline.Value, 0), new(limit + i * step + (float)timeline.Value, Size.Y), Colors.Red);
            }
        }

        //test
        DrawTrack(new(), Colors.CadetBlue);

        // Draw other curves.
        {
            // float scale = timeline->get_zoom_scale();
            // Ref<Texture2D> point = get_editor_theme_icon(SNAME("KeyValue"));
            // for (const KeyValue<int, Color> &E : subtrack_colors) {
            // 	if (hidden_tracks.has(E.key)) {
            // 		continue;
            // 	}
            // 	_draw_track(E.key, E.value);

            // 	for (int i = 0; i < animation->track_get_key_count(E.key); i++) {
            // 		float offset = animation->track_get_key_time(E.key, i);
            // 		float value = animation->bezier_track_get_key_value(E.key, i);

            // 		Vector2 pos((offset - timeline->get_value()) * scale + limit, _bezier_h_to_pixel(value));

            // 		if (pos.x >= limit && pos.x <= right_limit) {
            // 			draw_texture(point, pos - point->get_size() / 2.0, E.value);
            // 		}
            // 	}
            // }

            // if (track_count > 0 && !hidden_tracks.has(selected_track)) {
            // 	// Draw edited curve.
            // 	_draw_track(selected_track, selected_track_color);
            // }
        }
    }

    private void DrawTrack(BezierTrack track, Color color) {
        float scale = timeline.ZoomScale;

        float limit = timeline.NameLimit;
        float rightLimit = Size.X;

        for (int i = 0; i < track.timeValues.Count - 1; i++) {
            BezierPoint point = track.timeValues.Values[i];
            BezierPoint nextPoint = track.timeValues.Values[i + 1];

            float offset = (float)point.time;
            float height = (float)point.value;
            Vector2 outHandle = point.outHandle;

            // if (p_track == moving_handle_track && (moving_handle == -1 || moving_handle == 1) && moving_handle_key == i) {
            //     out_handle = moving_handle_right;
            // }

            // if (moving_selection && selection.has(IntPair(p_track, i))) {
            //     offset += moving_selection_offset.x;
            //     height += moving_selection_offset.y;
            // }

            outHandle += new Vector2(offset, height);

            float nextOffset = (float)nextPoint.time;
            float nextHeight = (float)nextPoint.value;
            Vector2 inHandle = nextPoint.inHandle;
            // if (p_track == moving_handle_track && (moving_handle == -1 || moving_handle == 1) && moving_handle_key == i_n) {
            //     in_handle = moving_handle_left;
            // }

            // if (moving_selection && selection.has(IntPair(p_track, i_n))) {
            //     offset_n += moving_selection_offset.x;
            //     height_n += moving_selection_offset.y;
            // }

            inHandle += new Vector2(nextOffset, nextHeight);

            Vector2 start = new(offset, height);
            Vector2 end = new(nextOffset, nextHeight);

            int fromX = (int)((offset - timeline.Value) * scale + limit);
            int pointStart = fromX;
            int toX = (int)((nextOffset - timeline.Value) * scale + limit);
            int pointEnd = toX;

            if (fromX > rightLimit) { // Not visible.
                continue;
            }

            if (toX < limit) { // Not visible.
                continue;
            }

            fromX = Mathf.Max(fromX, (int)limit);
            toX = Mathf.Min(toX, (int)rightLimit);

            List<Vector2> lines = [];
            Vector2 prevPos = new();

            for (int j = fromX; j <= toX; j++) {
                float t = (j - limit) / scale + (float)timeline.Value;

                float h;

                if (j == pointEnd) {
                    h = end.Y; // Make sure it always connects.
                } else if (j == pointStart) {
                    h = start.Y; // Make sure it always connects.
                } else { // Custom interpolation, used because it needs to show paths affected by moving the selection or handles.
                    int iterations = 10;
                    float low = 0;
                    float high = 1;

                    // Narrow high and low as much as possible.
                    for (int k = 0; k < iterations; k++) {
                        float middle = (low + high) / 2f;

                        Vector2 interp = start.BezierInterpolate(outHandle, inHandle, end, middle);

                        if (interp.X < t) {
                            low = middle;
                        } else {
                            high = middle;
                        }
                    }

                    // Interpolate the result.
                    Vector2 lowPos = start.BezierInterpolate(outHandle, inHandle, end, low);
                    Vector2 highPos = start.BezierInterpolate(outHandle, inHandle, end, high);

                    float c = (t - lowPos.X) / (highPos.X - lowPos.X);

                    h = lowPos.Lerp(highPos, c).Y;
                }

                h = BezierHToPixel(h);

                Vector2 pos = new(j, h);

                if (j > fromX) {
                    lines.Add(prevPos);
                    lines.Add(pos);
                }
                prevPos = pos;
            }

            if (lines.Count >= 2) {
                DrawMultiline([.. lines], color, Mathf.Round(/* EDSCALE */ 1), true);
            }
        }
    }

    private void PanCallback(Vector2 scrollVec, InputEvent @event) {
        if (@event is InputEventMouseMotion mm) {
            if (mm.Position.X > timeline.NameLimit) {
                timelineVScroll += scrollVec.Y * timelineVZoom;
                timelineVScroll = Mathf.Clamp(timelineVScroll, -100000, 100000);
                timeline.Value -= scrollVec.X / timeline.ZoomScale;
            } else {
                trackVScroll += scrollVec.Y;
                if (trackVScroll < -trackVScrollMax) {
                    trackVScroll = -trackVScrollMax;
                } else if (trackVScroll > 0) {
                    trackVScroll = 0;
                }
            }
            QueueRedraw();
        }
    }

    private void ZoomCallback(float zoomFactor, Vector2 origin, InputEvent @event) {
        float vZoomOrig = timelineVZoom;
        if (@event is InputEventWithModifiers iewm && iewm.AltPressed) {
            // Alternate zoom (doesn't affect timeline).
            timelineVZoom = Mathf.Clamp(timelineVZoom * zoomFactor, 0.000001f, 100000f);
        } else {
            float _zoomFactor = zoomFactor > 1.0 ? AnimationTrackTimelineEdit.ScrollZoomFactorIn : AnimationTrackTimelineEdit.ScrollZoomFactorOut;
            timeline.ZoomCallback(_zoomFactor, origin, @event);
        }
        timelineVScroll += (origin.Y - Size.Y / 2f) * (timelineVZoom - vZoomOrig);
        QueueRedraw();
    }

    private void ZoomChanged() {
        QueueRedraw();
        // palyposition
    }

    private float BezierHToPixel(float h) {
        h = (h - timelineVScroll) / timelineVZoom;
        h = (Size.Y / 2f) - h;
        return h;
    }
}
