﻿using MCBS.Event;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.UI
{
    public interface IControlEventHandling
    {
        public void HandleCursorMove(CursorEventArgs e);

        public bool HandleRightClick(CursorEventArgs e);

        public bool HandleLeftClick(CursorEventArgs e);

        public bool HandleCursorSlotChanged(CursorSlotEventArgs e);

        public bool HandleCursorItemChanged(CursorItemEventArgs e);

        public bool HandleTextEditorUpdate(CursorTextEventArgs e);

        public void HandleBeforeFrame(EventArgs e);

        public void HandleAfterFrame(EventArgs e);
    }
}
