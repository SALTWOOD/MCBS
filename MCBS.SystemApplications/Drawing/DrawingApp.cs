﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.SystemApplications.Drawing
{
    public class DrawingApp : Application
    {
        public const string ID = "Drawing";

        public const string Name = "绘画";

        public override object? Main(string[] args)
        {
            string? path = null;
            if (args.Length > 0)
                path = args[0];

            RunForm(new DrawingForm(path));
            return null;
        }
    }
}
