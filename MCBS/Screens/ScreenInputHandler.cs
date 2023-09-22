﻿using static MCBS.Config.ConfigManager;
using CoreRCON;
using QuanLib.Core.Event;
using QuanLib.Minecraft.Vector;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CoreRCON.Parsers.Standard;
using QuanLib.Minecraft.Selectors;
using QuanLib.Core;
using QuanLib.Minecraft.Command.Sender;
using QuanLib.Minecraft.Command;
using MCBS.Event;
using QuanLib.Minecraft;
using QuanLib.Minecraft.Snbt.Model;

namespace MCBS.Screens
{
    /// <summary>
    /// 屏幕输入处理
    /// </summary>
    public class ScreenInputHandler : ICursorReader, ITextEditor
    {
        public ScreenInputHandler(Screen owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            IsInitialState = true;
            CurrentPlayer = string.Empty;
            CurrenMode = CursorMode.Cursor;
            CurrentPosition = new(0, 0);
            CurrentSlot = 0;
            CurrentItem = null;
            InitialText = string.Empty;
            CurrentText = string.Empty;
            IdleTime = 0;

            CursorMove += OnCursorMove;
            RightClick += OnRightClick;
            LeftClick += OnLeftClick;
            CursorSlotChanged += OnCursorSlotChanged;
            CursorItemChanged += OnCursorItemChanged;
            TextEditorUpdate += OnTextEditorUpdate;
        }

        private const string MOUSE_ITEM = "minecraft:snowball";

        private const string TEXTEDITOR_ITEM = "minecraft:writable_book";

        private readonly Screen _owner;

        public bool IsInitialState { get; private set; }

        public CursorMode CurrenMode { get; private set; }

        public string CurrentPlayer { get; private set; }

        public Point CurrentPosition { get; private set; }

        public int CurrentSlot { get; private set; }

        public Item? CurrentItem { get; private set; }

        public string InitialText { get; set; }

        public string CurrentText { get; private set; }

        public int IdleTime { get; private set; }

        public event EventHandler<ICursorReader, CursorEventArgs> CursorMove;

        public event EventHandler<ICursorReader, CursorEventArgs> RightClick;

        public event EventHandler<ICursorReader, CursorEventArgs> LeftClick;

        public event EventHandler<ICursorReader, CursorSlotEventArgs> CursorSlotChanged;

        public event EventHandler<ICursorReader, CursorItemEventArgs> CursorItemChanged;

        public event EventHandler<ITextEditor, CursorTextEventArgs> TextEditorUpdate;

        protected virtual void OnCursorMove(ICursorReader sender, CursorEventArgs e) { }

        protected virtual void OnRightClick(ICursorReader sender, CursorEventArgs e) { }

        protected virtual void OnLeftClick(ICursorReader sender, CursorEventArgs e) { }

        protected virtual void OnCursorSlotChanged(ICursorReader sender, CursorSlotEventArgs e) { }

        protected virtual void OnCursorItemChanged(ICursorReader sender, CursorItemEventArgs e) { }

        protected virtual void OnTextEditorUpdate(ITextEditor sender, CursorTextEventArgs e) { }

        public void ResetText()
        {
            CurrentText = string.Empty;
            IsInitialState = true;
        }

        public void HandleInput()
        {
            Screen screen = _owner;
            CommandSender sender = MCOS.Instance.MinecraftInstance.CommandSender;

            Dictionary<string, EntityPos> playerPositions = sender.GetAllPlayerPosition();
            Func<IVector3<double>, double> GetDistance = screen.NormalFacing switch
            {
                Facing.Xp or Facing.Xm => (positions) => Math.Abs(positions.X - screen.PlaneCoordinate),
                Facing.Yp or Facing.Ym => (positions) => Math.Abs(positions.Y - screen.PlaneCoordinate),
                Facing.Zp or Facing.Zm => (positions) => Math.Abs(positions.Z - screen.PlaneCoordinate),
                _ => throw new InvalidOperationException(),
            };

            int length = screen.Width > screen.Height ? screen.Width : screen.Height;
            BlockPos center = screen.CenterPosition;
            Vector3<double> start = new(center.X - length, center.Y - length, center.Z - length);
            Vector3<double> range = new(length * 2, length * 2, length * 2);
            Bounds bounds = new(start, range);
            List<(string name, double distance)> distances = new();
            foreach (var playerPosition in playerPositions)
            {
                if (bounds.Contains(playerPosition.Value))
                    distances.Add((playerPosition.Key, GetDistance(playerPosition.Value)));
            }

            if (distances.Count == 0)
            {
                IdleTime++;
                return;
            }

            var orderDistances = distances.OrderBy(item => item.distance);
            foreach (var orderDistance in orderDistances)
            {
                if (HandlePlayer(orderDistance.name))
                {
                    IdleTime = 0;
                    return;
                }
            }

            IdleTime++;
            return;
        }

        private bool HandlePlayer(string player)
        {
            Screen screen = _owner;
            CommandSender sender = MCOS.Instance.MinecraftInstance.CommandSender;

            if (!sender.TryGetPlayerSelectedItemSlot(player, out var slot))
                return false;

            sender.TryGetPlayerItem(player, slot, out Item? mainItem);
            sender.TryGetPlayerDualWieldItem(player, out Item? dualItem);

            bool swap = false;
            if (mainItem is not null && (mainItem.ID == MOUSE_ITEM || mainItem.ID == TEXTEDITOR_ITEM))
            {

            }
            else if (dualItem is not null && (dualItem.ID == MOUSE_ITEM || dualItem.ID == TEXTEDITOR_ITEM))
            {
                Item? temp = mainItem;
                mainItem = dualItem;
                dualItem = temp;
                swap = true;
            }
            else
            {
                return false;
            }

            if (!sender.TryGetEntityPosition(player, out var position) || !sender.TryGetEntityRotation(player, out var rotation))
                return false;

            if (!EntityPos.CheckPlaneReachability(position, rotation, screen.NormalFacing, screen.PlaneCoordinate))
                return false;

            position.Y += 1.625;
            BlockPos targetBlock = EntityPos.GetToPlaneIntersection(position, rotation.ToDirection(), screen.NormalFacing, screen.PlaneCoordinate).ToBlockPos();
            Point targetPosition = screen.ToScreenPosition(targetBlock);
            if (!screen.IncludedOnScreen(targetPosition))
                return false;

            if (ScreenConfig.ScreenOperatorList.Count != 0 && !ScreenConfig.ScreenOperatorList.Contains(player))
            {
                sender.ShowActionbarTitle(player, "[屏幕输入模块] 错误：你没有权限控制屏幕", TextColor.Red);
                return false;
            }

            List<Action> actions = new();

            CurrentPlayer = player;

            switch (mainItem.ID)
            {
                case MOUSE_ITEM:
                    CurrenMode = CursorMode.Cursor;
                    break;
                case TEXTEDITOR_ITEM:
                    CurrenMode = CursorMode.TextEditor;
                    break;
            }

            if (targetPosition != CurrentPosition)
            {
                CurrentPosition = targetPosition;
                actions.Add(() => CursorMove.Invoke(this, new(CurrentPosition)));
            }

            if (swap && slot != CurrentSlot)
            {
                int temp = CurrentSlot;
                CurrentSlot = slot;
                actions.Add(() => CursorSlotChanged.Invoke(this, new(CurrentPosition, temp, CurrentSlot)));
            }

            if (!Item.EqualsID(dualItem, CurrentItem))
            {
                CurrentItem = dualItem;
                actions.Add(() => CursorItemChanged.Invoke(this, new(CurrentPosition, CurrentItem)));
            }

            switch (mainItem.ID)
            {
                case MOUSE_ITEM:
                    if (MCOS.Instance.InteractionManager.Items.TryGetValue(CurrentPlayer, out var interaction))
                    {
                        if (interaction.IsLeftClick)
                            actions.Add(() => LeftClick.Invoke(this, new(CurrentPosition)));
                        if (interaction.IsRightClick)
                            actions.Add(() => RightClick.Invoke(this, new(CurrentPosition)));
                    }
                    else
                    {
                        int score = sender.GetPlayerScoreboard(CurrentPlayer, ScreenConfig.RightClickObjective);
                        if (score > 0)
                        {
                            actions.Add(() => RightClick.Invoke(this, new(CurrentPosition)));
                            sender.SetPlayerScoreboard(CurrentPlayer, ScreenConfig.RightClickObjective, 0);
                        }
                    }
                    break;
                case TEXTEDITOR_ITEM:
                    if (IsInitialState)
                    {
                        if (string.IsNullOrEmpty(InitialText))
                            sender.SetPlayerHotbarItem(CurrentPlayer, mainItem.Slot, $"minecraft:writable_book{{pages:[]}}");
                        else
                            sender.SetPlayerHotbarItem(CurrentPlayer, mainItem.Slot, $"minecraft:writable_book{{pages:[\"{InitialText}\"]}}");
                        CurrentText = InitialText;
                        IsInitialState = false;
                    }
                    else if (
                        mainItem.Tag is not null &&
                        mainItem.Tag.TryGetValue("pages", out var pagesTag) &&
                        pagesTag is string[] texts && texts.Length > 0)
                    {
                        if (texts[0] != CurrentText)
                        {
                            CurrentText = texts[0];
                            actions.Add(() => TextEditorUpdate.Invoke(this, new(CurrentPosition, CurrentText)));
                        }
                    }
                    else if (!string.IsNullOrEmpty(CurrentText))
                    {
                        CurrentText = string.Empty;
                        actions.Add(() => TextEditorUpdate.Invoke(this, new(CurrentPosition, CurrentText)));
                    }
                    break;
            }

            foreach (var action in actions)
                action.Invoke();

            return true;
        }
    }
}
