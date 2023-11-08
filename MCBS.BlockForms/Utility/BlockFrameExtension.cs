﻿using MCBS.BlockForms.Utility;
using MCBS.Rendering;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.BlockForms.Utility
{
    public static class BlockFrameExtension
    {
        public static OverwriteContext Overwrite(this BlockFrame source, BlockFrame blockFrame, AnchorPosition anchorPosition)
        {
            if (blockFrame is null)
                throw new ArgumentNullException(nameof(blockFrame));

            Point location = anchorPosition switch
            {
                AnchorPosition.UpperLeft => Point.Empty,
                AnchorPosition.UpperRight => new(source.Width - blockFrame.Width, 0),
                AnchorPosition.LowerLeft => new(0, source.Height - blockFrame.Height),
                AnchorPosition.LowerRight => new(source.Width - blockFrame.Width, source.Height - blockFrame.Height),
                AnchorPosition.Centered => new(source.Width / 2 - blockFrame.Width / 2, source.Height / 2 - blockFrame.Height / 2),
                _ => throw new InvalidOperationException(),
            };

            return source.Overwrite(blockFrame, location);
        }
    }
}
