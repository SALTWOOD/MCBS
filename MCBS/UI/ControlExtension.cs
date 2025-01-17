﻿using MCBS.Events;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.UI
{
    public static class ControlExtension
    {
        public static int IndexOf<T>(this IReadOnlyControlCollection<T> source, T item) where T : class, IControl
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == item)
                    return i;
            }
            return -1;
        }

        public static IForm? GetForm(this IControl source)
        {
            IControl? result = source;
            while (true)
            {
                if (result is null)
                    return null;
                else if (result is IForm form)
                    return form;
                else
                    result = result.GenericParentContainer;
            }
        }

        public static IRootForm? GetRootForm(this IControl source)
        {
            IControl? result = source;
            while (true)
            {
                if (result is null)
                    return null;
                else if (result is IRootForm form)
                    return form;
                else
                    result = result.GenericParentContainer;
            }
        }

        public static Point GetRenderingLocation(this IControlRendering source)
        {
            return new(source.ClientLocation.X + source.BorderWidth, source.ClientLocation.Y + source.BorderWidth);
        }

        public static Point ParentPos2ChildPos(this IControl source, Point position)
        {
            return new(position.X - source.ClientLocation.X + source.OffsetPosition.X - source.BorderWidth, position.Y - source.ClientLocation.Y + source.OffsetPosition.Y - source.BorderWidth);
        }

        public static Point ChildPos2ParentPos(this IControl source, Point position)
        {
            return new(position.X + source.ClientLocation.X + source.OffsetPosition.X + source.BorderWidth, position.Y + source.ClientLocation.Y + source.OffsetPosition.Y + source.BorderWidth);
        }

        public static bool IncludedOnControl(this IControl source, Point position)
        {
            position.X -= source.OffsetPosition.X;
            position.Y -= source.OffsetPosition.Y;
            return position.X >= 0 && position.Y >= 0 && position.X < source.ClientSize.Width && position.Y < source.ClientSize.Height;
        }

        public static void UpdateAllHoverState(this IContainerControl source, CursorEventArgs e)
        {
            foreach (var control in source.GetChildControls().ToArray())
            {
                if (control is IContainerControl containerControl)
                    containerControl.UpdateAllHoverState(e.Clone(control.ParentPos2ChildPos));
                else
                    control.UpdateHoverState(e.Clone(control.ParentPos2ChildPos));
            }

            source.UpdateHoverState(e);
        }
    }
}
