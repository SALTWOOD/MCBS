﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.SystemApplications.Settings
{
    public class SettingsApp : Application
    {
        public const string ID = "Settings";

        public const string Name = "设置";

        public override object? Main(string[] args)
        {
            RunForm(new SettingsForm());
            return null;
        }
    }
}