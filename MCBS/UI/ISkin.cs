﻿using MCBS.Frame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.UI
{
    public interface ISkin
    {
        public string GetForegroundBlockID();

        public string GetBackgroundBlockID();

        public string GetBorderBlockID();

        public ImageFrame? GetBackgroundImage();
    }
}
