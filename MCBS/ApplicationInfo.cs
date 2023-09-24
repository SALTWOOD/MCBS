﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MCBS
{
    public abstract class ApplicationInfo
    {
        static ApplicationInfo()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MCBS.SystemResource.DefaultIcon.png") ?? throw new InvalidOperationException();
            _defaultIcon = Image.Load<Rgba32>(stream);
        }

        protected ApplicationInfo(Type typeObject)
        {
            TypeObject = typeObject;
        }

        private readonly static Image<Rgba32> _defaultIcon;

        public static Image<Rgba32> DefaultIcon => _defaultIcon.Clone();

        public Type TypeObject { get; }

        public abstract PlatformID[] Platforms { get; }

        public abstract string ID { get; }

        public abstract string Name { get; }

        public abstract Version Version { get; }

        public abstract Image<Rgba32> Icon { get; }

        public abstract bool AppendToDesktop { get; }

        public string GetApplicationDirectory()
        {
            return MCOS.MainDirectory.Applications.GetApplicationDirectory(ID);
        }

        public override string ToString()
        {
            return $"Name={Name}, ID={ID}, Version={Version}";
        }
    }

    public abstract class ApplicationInfo<T> : ApplicationInfo where T : Application
    {
        protected ApplicationInfo() : base(typeof(T)) { }
    }
}
