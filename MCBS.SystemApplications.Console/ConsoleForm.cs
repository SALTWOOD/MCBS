﻿using MCBS.BlockForms;
using MCBS.BlockForms.Utility;
using MCBS.Events;
using QuanLib.Minecraft.Blocks;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBS.SystemApplications.Console
{
    public class ConsoleForm : WindowForm
    {
        public ConsoleForm(string? path = null)
        {
            _open = path;
            Output_MultilineTextBox = new();
            Input_TextBox = new();
            Send_Button = new();

            ProcessInfo processInfo;
            if (path is null)
            {
                processInfo = ProcessInfo.CMD;
            }
            else
            {
                try
                {
                    processInfo = ProcessInfo.ReadJsonFile(path);
                }
                catch
                {
                    processInfo = ProcessInfo.CMD;
                }
            }
            
            _consoleProcess = new(processInfo);
        }

        private readonly string? _open;

        private readonly ConsoleProcess _consoleProcess;

        private readonly MultilineTextBox Output_MultilineTextBox;

        private readonly TextBox Input_TextBox;

        private readonly Button Send_Button;

        public override void Initialize()
        {
            base.Initialize();

            ClientPanel_Control.ChildControls.Add(Output_MultilineTextBox);
            Output_MultilineTextBox.WordWrap = false;
            Output_MultilineTextBox.EnableHorizontalSliding = true;
            Output_MultilineTextBox.AutoScroll = true;
            Output_MultilineTextBox.IsReadOnly = true;
            Output_MultilineTextBox.BorderWidth = 0;
            Output_MultilineTextBox.ClientSize = new(ClientPanel_Control.ClientSize.Width, ClientPanel_Control.ClientSize.Height - 18);
            Output_MultilineTextBox.Stretch = Direction.Bottom | Direction.Right;
            Output_MultilineTextBox.Skin.SetAllBackgroundColor(BlockManager.Concrete.Black);
            Output_MultilineTextBox.Skin.SetAllForegroundColor(BlockManager.Concrete.White);
            Output_MultilineTextBox.AfterFrame += Output_MultilineTextBox_AfterFrame;

            ClientPanel_Control.ChildControls.Add(Input_TextBox);
            Input_TextBox.Size = new(ClientSize.Width - 34, 18);
            Input_TextBox.Stretch = Direction.Right;
            Input_TextBox.Anchor = Direction.Bottom | Direction.Right;
            Input_TextBox.LayoutDown(ClientPanel_Control, Output_MultilineTextBox, 0);

            ClientPanel_Control.ChildControls.Add(Send_Button);
            Send_Button.Text = "Send";
            Send_Button.ClientSize = new(32, 16);
            Send_Button.Anchor = Direction.Bottom | Direction.Right;
            Send_Button.LayoutRight(ClientPanel_Control, Input_TextBox, 0);
            Send_Button.RightClick += Send_Button_RightClick;

            _consoleProcess.Start("Console Thread");
        }

        private void Output_MultilineTextBox_AfterFrame(Control sender, EventArgs e)
        {
            string output = _consoleProcess.GetOutputText();
            if (!string.IsNullOrEmpty(output))
            {
                Output_MultilineTextBox.Text += output;
                return;
            }

            string error = _consoleProcess.GetErrorText();
            if (!string.IsNullOrEmpty(error))
            {
                Output_MultilineTextBox.Text += error;
                return;
            }
        }

        private void Send_Button_RightClick(Control sender, CursorEventArgs e)
        {
            if (string.IsNullOrEmpty(Input_TextBox.Text))
                return;

            _consoleProcess.Process.StandardInput.WriteLine(Input_TextBox.Text);
            Input_TextBox.Text = string.Empty;
        }

        protected override void DisposeUnmanaged()
        {
            base.DisposeUnmanaged();

            _consoleProcess.Dispose();
        }
    }
}
