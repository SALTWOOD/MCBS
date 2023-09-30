﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCBS.Application;

namespace MCBS.SystemApplications.ScreenManager
{
    public class ScreenManagerApp : ApplicationBase
    {
        public const string ID = "ScreenManager";

        public const string Name = "屏幕管理器";

        public override object? Main(string[] args)
        {
            RunForm(new ScreenManagerForm());
            return null;
        }
    }
}
