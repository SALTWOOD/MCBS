﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS
{
    [Flags]
    public enum PlatformType
    {
        None = 0,

        Windows = 1,

        Linux = 2,

        MacOS = 4
    }
}
