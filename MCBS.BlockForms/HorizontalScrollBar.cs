﻿using MCBS.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.BlockForms
{
    public class HorizontalScrollBar : ScrollBar
    {
        protected override BlockFrame Rendering()
        {
            int position = (int)Math.Round(ClientSize.Width * SliderPosition);
            int length = (int)Math.Round(ClientSize.Width * SliderSize);
            if (length < 1)
                length = 1;

            BlockFrame baseFrame = base.Rendering();
            baseFrame.Overwrite(new HashBlockFrame(length, ClientSize.Height, GetForegroundColor()), new(position, 0));
            return baseFrame;
        }
    }
}
