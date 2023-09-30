﻿using MCBS;
using MCBS.BlockForms.Utility;
using MCBS.Events;
using QuanLib.Core.Events;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.BlockForms
{
    public class IconTextBox : ContainerControl<Control>
    {
        public IconTextBox()
        {
            Icon_PictureBox = new();
            Icon_PictureBox.BorderWidth = 0;
            Icon_PictureBox.ClientSize = new(16, 16);
            Icon_PictureBox.Skin.SetAllBackgroundBlockID(string.Empty);

            Text_Label = new();
            Text_Label.Skin.SetAllBackgroundBlockID(string.Empty);

            _Spacing = 0;

            ClientSize = new(SR.DefaultFont.HalfWidth * 6, SR.DefaultFont.Height);
        }

        public readonly PictureBox Icon_PictureBox;

        public readonly Label Text_Label;

        public int Spacing
        {
            get => _Spacing;
            set
            {
                if (value < 0)
                    value = 0;
                if (_Spacing != value)
                {
                    _Spacing = value;
                    RequestUpdateFrame();
                }
            }
        }
        private int _Spacing;

        public override void Initialize()
        {
            base.Initialize();

            ChildControls.Add(Icon_PictureBox);
            Icon_PictureBox.ImageFrameChanged += Icon_PictureBox_ImageFrameChanged;

            ChildControls.Add(Text_Label);
            Text_Label.TextChanged += Text_Label_TextChanged;

            ActiveLayoutAll();
        }

        private void Icon_PictureBox_ImageFrameChanged(PictureBox sender, ImageFrameChangedEventArgs e)
        {
            if (AutoSize)
                AutoSetSize();
        }

        private void Text_Label_TextChanged(Control sender, TextChangedEventArgs e)
        {
            if (AutoSize)
                AutoSetSize();
        }

        public override void ActiveLayoutAll()
        {
            Icon_PictureBox.LayoutVerticalCentered(this, Spacing);
            Text_Label.LayoutVerticalCentered(this, Icon_PictureBox.RightLocation + 1);
        }

        public override void AutoSetSize()
        {
            Size size = SR.DefaultFont.GetTotalSize(Text_Label.Text);
            size.Width += Icon_PictureBox.ImageFrame.FrameSize.Width;
            if (Icon_PictureBox.ImageFrame.FrameSize.Height > size.Height)
            {
                size.Height = Math.Max(size.Height, Icon_PictureBox.ImageFrame.FrameSize.Height);
            }
            size.Width += Spacing * 2;
            size.Height += Spacing * 2;
            ClientSize = size;
            ActiveLayoutAll();
        }
    }
}
