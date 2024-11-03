/**************************************************************************/
/*  view_panner.h, view_panner.cpp                                        */
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
using PCE.Util;

namespace PCE.Editor;

public partial class ViewPanner : RefCounted {
    public delegate void ZoomCallback(float zoom, Vector2 origin, InputEvent @event);
    public delegate void PanCallback(Vector2 scrollVec, InputEvent @event);

    public enum ControlSchemeEnum {
        ScrollZooms,
        ScrollPans
    }

    public enum PanAxisEnum {
        Both,
        Horizontal,
        Vertical
    }

    private int scrollSpeed = 32;
    private float scrollZoomFactor = 1.1f;
    public PanAxisEnum PanAxis { get; set; } = PanAxisEnum.Both;

    private bool isDragging = false;
    private bool panKeyPressed = false;
    public bool ForceDrag { get; set; } = false;

    public bool EnableRmb { get; set; } = false;
    public bool SimplePanningEnabled { get; set; } = false;

    private Shortcut panViewShortcut;

    private Callable panCallback;
    private Callable zoomCallback;

    public ControlSchemeEnum ControlScheme { get; set; } = ControlSchemeEnum.ScrollZooms;
    public bool Panning => isDragging || panKeyPressed;

    public bool GuiInput(InputEvent @event, Rect2 canvasRect = new Rect2()) {
        if (@event is InputEventMouseButton mb) {
            Vector2 scrollVec = new(
                (mb.ButtonIndex == MouseButton.WheelRight ? 1f : 0) - (mb.ButtonIndex == MouseButton.WheelLeft ? 1f : 0),
                (mb.ButtonIndex == MouseButton.WheelDown ? 1f : 0) - (mb.ButtonIndex == MouseButton.WheelUp ? 1f : 0)
            );
            // Moving the scroll wheel sends two events: one with pressed as true,
            // and one with pressed as false. Make sure we only process one of them.
            if (scrollVec != new Vector2() && mb.Pressed) {
                if (ControlScheme == ControlSchemeEnum.ScrollPans) {
                    if (mb.CtrlPressed) {
                        if (scrollVec.Y != 0) {
                            // Compute the zoom factor.
                            float zoomFactor = mb.Factor <= 0 ? 1f : mb.Factor;
                            zoomFactor = ((scrollZoomFactor - 1f) * zoomFactor) + 1f;
                            float zoom = scrollVec.Y > 0 ? 1f / scrollZoomFactor : scrollZoomFactor;
                            zoomCallback.Call(zoom, mb.Position, @event);
                            return true;
                        }
                    } else {
                        Vector2 panning = scrollVec * mb.Factor;
                        if (PanAxis == PanAxisEnum.Horizontal) {
                            panning = new(panning.X + panning.Y, 0);
                        } else if (PanAxis == PanAxisEnum.Vertical) {
                            panning = new(0, panning.X + panning.Y);
                        } else if (mb.ShiftPressed) {
                            panning = new(panning.Y, panning.X);
                        }
                        panCallback.Call(-panning * scrollSpeed, @event);
                        return true;
                    }
                } else {
                    if (mb.CtrlPressed) {
                        Vector2 panning = scrollVec * mb.Factor;
                        if (PanAxis == PanAxisEnum.Horizontal) {
                            panning = new(panning.X + panning.Y, 0);
                        } else if (PanAxis == PanAxisEnum.Vertical) {
                            panning = new(0, panning.X + panning.Y);
                        } else if (mb.ShiftPressed) {
                            panning = new(panning.Y, panning.X);
                        }
                        panCallback.Call(-panning * scrollSpeed, @event);
                        return true;
                    } else if (!mb.ShiftPressed && scrollVec.Y != 0) {
                        // Compute the zoom factor.
                        float zoomFactor = mb.Factor <= 0 ? 1f : mb.Factor;
                        zoomFactor = ((scrollZoomFactor - 1f) * zoomFactor) + 1f;
                        float zoom = scrollVec.Y > 0 ? 1f / scrollZoomFactor : scrollZoomFactor;
                        zoomCallback.Call(zoom, mb.Position, @event);
                        return true;
                    }
                }
            }

            // Alt is not used for button presses, so ignore it.
            if (mb.AltPressed) {
                return false;
            }

            bool isDragEvent = mb.ButtonIndex == MouseButton.Middle ||
                (EnableRmb && mb.ButtonIndex == MouseButton.Right) ||
                (!SimplePanningEnabled && mb.ButtonIndex == MouseButton.Left && Panning) ||
                (ForceDrag && mb.ButtonIndex == MouseButton.Left);

            if (isDragEvent) {
                if (mb.Pressed) {
                    isDragging = true;
                } else {
                    isDragging = false;
                }
                return mb.ButtonIndex != MouseButton.Left || mb.Pressed; // Don't consume LMB release events (it fixes some selection problems).
            }
        }

        if (@event is InputEventMouseMotion mm) {
            if (isDragging) {
                if (canvasRect != new Rect2()) {
                    panCallback.Call(mm.WrapMouseMotion(canvasRect), @event);
                } else {
                    panCallback.Call(mm.Relative, @event);
                }
                return true;
            }
        }

        if (@event is InputEventMagnifyGesture magnifyGesture) {
            // Zoom gesture
            zoomCallback.Call(magnifyGesture.Factor, magnifyGesture.Position, @event);
            return true;
        }

        if (@event is InputEventPanGesture panGesture) {
            if (panGesture.CtrlPressed) {
                // Zoom gesture.
                float panZoomFactor = 1.02f;
                float zoomDirection = panGesture.Delta.X - panGesture.Delta.Y;
                if (zoomDirection == 0f) {
                    return true;
                }
                float zoom = zoomDirection < 0 ? 1.0f / panZoomFactor : panZoomFactor;
                zoomCallback.Call(zoom, panGesture.Position, @event);
                return true;
            }
            panCallback.Call(-panGesture.Delta * scrollSpeed, @event);
        }

        if (@event is InputEventScreenDrag screenDrag) {
            if (Input.EmulateMouseFromTouch || Input.EmulateTouchFromMouse) {
                // This set of events also generates/is generated by
                // InputEventMouseButton/InputEventMouseMotion events which will be processed instead.
            } else {
                panCallback.Call(screenDrag.Relative, @event);
            }
        }

        if (@event is InputEventKey k) {
            if (panViewShortcut is not null && panViewShortcut.MatchesEvent(k)) {
                panKeyPressed = k.Pressed;
                if (SimplePanningEnabled || Input.GetMouseButtonMask().HasFlag(MouseButtonMask.Left)) {
                    isDragging = panKeyPressed;
                }
                return true;
            }
        }

        return false;
    }
    public void ReleasePanKey() {
        panKeyPressed = false;
        isDragging = false;
    }

    public void SetCallbacks(Callable panCallback, Callable zoomCallback) {
        this.panCallback = panCallback;
        this.zoomCallback = zoomCallback;
    }
    public void SetCallbacks(PanCallback panCallback, ZoomCallback zoomCallback) {
        this.panCallback = Callable.From(new Action<Vector2, InputEvent>(panCallback));
        this.zoomCallback = Callable.From(new Action<float, Vector2, InputEvent>(zoomCallback));
    }
    public void SetPanShortcut(Shortcut shortcut) {
        panViewShortcut = shortcut;
        panKeyPressed = false;
    }
    public void SetScrollSpeed(int scrollSpeed) {
        if (scrollSpeed <= 0)
            throw new ArgumentOutOfRangeException(nameof(scrollSpeed), scrollSpeed, "must be larger than 0");
        this.scrollSpeed = scrollSpeed;
    }
    public void SetScrollZoomFactor(float scrollZoomFactor) {
        if (scrollZoomFactor <= 1f)
            throw new ArgumentOutOfRangeException(nameof(scrollZoomFactor), scrollZoomFactor, "must be larger than 1");
        this.scrollZoomFactor = scrollZoomFactor;
    }

    public void Setup(ControlSchemeEnum scheme, Shortcut shortcut, bool simplePanning) {
        ControlScheme = scheme;
        SetPanShortcut(shortcut);
        SimplePanningEnabled = simplePanning;
    }

    public ViewPanner() {
        panViewShortcut = new() {
            Events = [new InputEventKey() { Keycode = Key.Space }]
        };
    }
}