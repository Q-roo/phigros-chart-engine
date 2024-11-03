using System;
using DotNext;
using Godot;

namespace PCE.Util;
public static class CommonExtensions {
    public static Result<U, R> AndThen<T, U, R>(this Result<T, R> result, System.Func<T, U> func) where R : struct, Enum {
        return result ? func(result.Value) : new Result<U, R>(result.Error);
    }

    public static void AndThen<T, R>(this Result<T, R> result, Action<T> action) where R : struct, Enum {
        if (result)
            action(result.Value);
    }

    public static bool IsNumericType(this object o) {
        return Type.GetTypeCode(o.GetType()) switch {
            TypeCode.Byte or
            TypeCode.SByte or
            TypeCode.UInt16 or
            TypeCode.UInt32 or
            TypeCode.UInt64 or
            TypeCode.Int16 or
            TypeCode.Int32 or
            TypeCode.Int64 or
            TypeCode.Decimal or
            TypeCode.Double or
            TypeCode.Single => true,
            _ => false,
        };
    }

    public static Viewport GetRootViewport(this Node control) {
        Viewport viewport = control.GetViewport();
        Node parent = viewport?.GetParent();

        while (parent is not null) {
            Viewport _viewport = parent.GetViewport();
            if (_viewport is null)
                return viewport;

            viewport = _viewport;
            parent = viewport.GetParent();
        }

        return viewport;
    }

    public static bool IsButtonPressed(this InputEventMouseMotion mouseMotion, MouseButtonMask button) {
        // return (mouseMotion.ButtonMask & button) != 0;
        return mouseMotion.ButtonMask.HasFlag(button);
    }

    /**************************************************************************/
    /*  input.cpp                                                             */
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

    public static Vector2 WrapMouseMotion(this InputEventMouseMotion motion, Rect2 rect) {
        // The relative distance reported for the next event after a warp is in the boundaries of the
        // size of the rect on that axis, but it may be greater, in which case there's no problem as fmod()
        // will warp it, but if the pointer has moved in the opposite direction between the pointer relocation
        // and the subsequent event, the reported relative distance will be less than the size of the rect
        // and thus fmod() will be disabled for handling the situation.
        // And due to this mouse warping mechanism being stateless, we need to apply some heuristics to
        // detect the warp: if the relative distance is greater than the half of the size of the relevant rect
        // (checked per each axis), it will be considered as the consequence of a former pointer warp.

        Vector2 rel_sign = new(motion.Relative.X >= 0.0f ? 1 : -1, motion.Relative.Y >= 0.0 ? 1 : -1);
        Vector2 warp_margin = rect.Size * 0.5f;
        Vector2 rel_warped = new(
            motion.Relative.X + rel_sign.X * warp_margin.X % rect.Size.X - rel_sign.X * warp_margin.X,
            motion.Relative.Y + rel_sign.Y * warp_margin.Y % rect.Size.Y - rel_sign.Y * warp_margin.Y
        );

        Vector2 pos_local = motion.GlobalPosition - rect.Position;
        Vector2 pos_warped = new(Mathf.PosMod(pos_local.X, rect.Size.X), Mathf.PosMod(pos_local.Y, rect.Size.Y));
        if (pos_warped != pos_local) {
            Input.WarpMouse(pos_warped + rect.Position);
        }

        return rel_warped;
    }
}