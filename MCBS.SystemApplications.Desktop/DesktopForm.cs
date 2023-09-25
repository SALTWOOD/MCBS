﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using MCBS.BlockForms;
using MCBS.Event;
using MCBS.BlockForms.Utility;
using MCBS;
using System.Reflection;

namespace MCBS.SystemApplications.Desktop
{
    public class DesktopForm : Form
    {
        static DesktopForm()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".SystemResource.DefaultWallpaper.jpg") ?? throw new InvalidOperationException();
            _defaultWallpaper = Image.Load<Rgba32>(stream);
        }

        public DesktopForm()
        {
            AllowMove = false;
            AllowResize = false;
            DisplayPriority = int.MinValue;
            MaxDisplayPriority = int.MinValue + 1;
            BorderWidth = 0;

            ClientPanel = new();
        }

        private static readonly Image<Rgba32> _defaultWallpaper;

        public readonly ClientPanel ClientPanel;

        public override void Initialize()
        {
            base.Initialize();

            SubControls.Add(ClientPanel);
            ClientPanel.ClientSize = ClientSize;
            ClientPanel.LayoutSyncer = new(this, (sender, e) => { }, (sender, e) =>
            ClientPanel.ClientSize = ClientSize);
            ClientPanel.LayoutAll += ClientPanel_LayoutAll;

            ActiveLayoutAll();

            ClientPanel.Skin.SetAllBackgroundImage(_defaultWallpaper);
        }

        public override void ActiveLayoutAll()
        {
            MCOS os = MCOS.Instance;
            ClientPanel.SubControls.Clear();
            foreach (var app in os.ApplicationManager.Items.Values)
                if (app.AppendToDesktop)
                    ClientPanel.SubControls.Add(new DesktopIcon(app));

            if (ClientPanel.SubControls.Count == 0)
                return;

            ClientPanel.ForceFillRightLayout(0, ClientPanel.SubControls);
            ClientPanel.PageSize = new((ClientPanel.SubControls.RecentlyAddedControl ?? ClientPanel.SubControls[^1]).RightLocation + 1, ClientPanel.ClientSize.Height);
            ClientPanel.OffsetPosition = new(0, 0);
            ClientPanel.RefreshHorizontalScrollBar();
        }

        [Obsolete("暂时无法设置壁纸", true)]
        public void SetAsWallpaper(Image<Rgba32> image)
        {
            throw new NotSupportedException();
        }

        private void ClientPanel_LayoutAll(AbstractContainer<Control> sender, SizeChangedEventArgs e)
        {
            ActiveLayoutAll();
        }
    }
}