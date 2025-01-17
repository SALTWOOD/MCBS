﻿using MCBS.BlockForms.Utility;
using MCBS.Events;
using MCBS.Forms;
using MCBS.UI;
using QuanLib.Minecraft.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.BlockForms
{
    public abstract partial class RootForm
    {
        public class TaskBar : ContainerControl<Control>
        {
            public TaskBar(RootForm owner)
            {
                ArgumentNullException.ThrowIfNull(owner, nameof(owner));

                _owner = owner;

                BorderWidth = 0;
                Height = 18;
                LayoutSyncer = new(_owner, (sender, e) => { }, (sender, e) =>
                {
                    Width = e.NewSize.Width;
                    ClientLocation = new(0, e.NewSize.Height - Height);
                });
                Skin.SetAllBackgroundColor(BlockManager.Concrete.White);

                StartMenu_Switch = new();
                FormsMenu = new();
                FullScreen_Button = new();
            }

            private readonly RootForm _owner;

            private readonly Switch StartMenu_Switch;

            private readonly Button FullScreen_Button;

            private readonly TaskBarIconMenu FormsMenu;

            public override void Initialize()
            {
                base.Initialize();

                if (_owner != ParentContainer)
                    throw new InvalidOperationException();

                ChildControls.Add(StartMenu_Switch);
                StartMenu_Switch.BorderWidth = 0;
                StartMenu_Switch.ClientLocation = new(0, 1);
                StartMenu_Switch.ClientSize = new(16, 16);
                StartMenu_Switch.Anchor = Direction.Bottom | Direction.Left;
                StartMenu_Switch.IsRenderingTransparencyTexture = false;
                StartMenu_Switch.Skin.SetBackgroundColor(Skin.BackgroundColor, ControlState.None, ControlState.Hover);
                StartMenu_Switch.Skin.SetBackgroundColor(BlockManager.Concrete.Orange, ControlState.Selected, ControlState.Hover | ControlState.Selected);
                StartMenu_Switch.Skin.SetAllBackgroundTexture(TextureManager.Instance["Logo"]);
                StartMenu_Switch.ControlSelected += StartMenu_Switch_ControlSelected;
                StartMenu_Switch.ControlDeselected += StartMenu_Switch_ControlDeselected; ;

                ChildControls.Add(FullScreen_Button);
                FullScreen_Button.BorderWidth = 0;
                FullScreen_Button.ClientSize = new(16, 16);
                FullScreen_Button.LayoutLeft(this, 1, 0);
                FullScreen_Button.Anchor = Direction.Bottom | Direction.Right;
                FullScreen_Button.IsRenderingTransparencyTexture = false;
                FullScreen_Button.Skin.SetBackgroundColor(Skin.BackgroundColor, ControlState.None, ControlState.Selected);
                FullScreen_Button.Skin.SetBackgroundColor(BlockManager.Concrete.LightGray, ControlState.Hover, ControlState.Hover | ControlState.Selected);
                FullScreen_Button.Skin.SetAllBackgroundTexture(TextureManager.Instance["Expand"]);
                FullScreen_Button.RightClick += HideTitleBar_Button_RightClick;

                ChildControls.Add(FormsMenu);
                FormsMenu.Spacing = 0;
                FormsMenu.MinWidth = 18;
                FormsMenu.BorderWidth = 0;
                FormsMenu.ClientSize = new(ClientSize.Width - StartMenu_Switch.Width - FullScreen_Button.Width, ClientSize.Height);
                FormsMenu.ClientLocation = new(StartMenu_Switch.RightLocation + 1, 0);
                FormsMenu.Stretch = Direction.Right;

                _owner.FormContainer_Control.AddedChildControl += FormContainer_AddedChildControl;
                _owner.FormContainer_Control.RemovedChildControl += FormContainer_RemovedChildControl;
            }

            public void SwitchSelectedForm(IForm form)
            {
                FormsMenu.SwitchSelectedForm(form);
            }

            private void StartMenu_Switch_ControlSelected(Control sender, EventArgs e)
            {
                _owner.ChildControls.TryAdd(_owner.StartMenu_ListMenuBox);

                _owner.StartMenu_ListMenuBox.ClientLocation = new(0, Math.Max(_owner.ClientSize.Height - _owner.TaskBar_Control.Height - _owner.StartMenu_ListMenuBox.Height, 0));
                if (_owner.StartMenu_ListMenuBox.BottomToBorder < _owner.TaskBar_Control.Height)
                    _owner.StartMenu_ListMenuBox.BottomToBorder = _owner.TaskBar_Control.Height;

                if (MCOS.Instance.ScreenContextOf(_owner)?.Screen.TestLight() ?? false)
                    _owner.Light_Switch.IsSelected = false;
                else
                    _owner.Light_Switch.IsSelected = true;
            }

            private void StartMenu_Switch_ControlDeselected(Control sender, EventArgs e)
            {
                _owner.ChildControls.Remove(_owner.StartMenu_ListMenuBox);
            }

            private void FormContainer_AddedChildControl(AbstractControlContainer<IControl> sender, ControlEventArgs<IControl> e)
            {
                if (e.Control is not IForm form)
                    return;

                var applicationManifest = MCOS.Instance.ProcessContextOf(form)?.Application;
                if (applicationManifest is null || applicationManifest.IsBackground)
                    return;

                var context = MCOS.Instance.FormContextOf(form);
                if (context is null)
                    return;

                switch (context.StateManager.CurrentState)
                {
                    case FormState.NotLoaded:
                    case FormState.Dragging:
                        FormsMenu.AddedChildControlAndLayout(new TaskBarIcon(form));
                        break;
                    case FormState.Minimize:
                        var icon = FormsMenu.TaskBarIconOf(form);
                        if (icon is not null)
                            icon.IsSelected = true;
                        break;
                }
            }

            private void FormContainer_RemovedChildControl(AbstractControlContainer<IControl> sender, ControlEventArgs<IControl> e)
            {
                if (e.Control is not IForm form)
                    return;

                var context = MCOS.Instance.FormContextOf(form);
                var icon = FormsMenu.TaskBarIconOf(form);
                if (context is null || icon is null)
                    return;

                switch (context.StateManager.NextState)
                {
                    case FormState.Minimize:
                        icon.IsSelected = false;
                        break;
                    case FormState.Dragging:
                    case FormState.Closed:
                        FormsMenu.RemoveChildControlAndLayout(icon);
                        break;
                }
            }

            private void HideTitleBar_Button_RightClick(Control sender, CursorEventArgs e)
            {
                _owner.ShowTaskBar = false;
            }
        }
    }
}
