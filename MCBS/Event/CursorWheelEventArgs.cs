﻿using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.Event
{
    public class CursorWheelEventArgs : CursorEventArgs
    {
        public CursorWheelEventArgs(Point position, int delta) : base(position)
        {
            Delta = delta;
        }

        public int Delta;
    }
}
