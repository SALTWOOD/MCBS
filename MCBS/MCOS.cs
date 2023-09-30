﻿#define TryCatch

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using QuanLib.BDF;
using SixLabors.ImageSharp;
using FFMediaToolkit;
using System.Collections.Concurrent;
using log4net.Core;
using System.Runtime.CompilerServices;
using QuanLib.Core;
using QuanLib.Minecraft.Instance;
using MCBS.Screens;
using MCBS.Frame;
using MCBS.Logging;
using MCBS.UI;
using MCBS.Config;
using MCBS.Processes;
using MCBS.Interactions;
using MCBS.Application;
using MCBS.Forms;

namespace MCBS
{
    public class MCOS : UnmanagedRunnable
    {
        private static readonly LogImpl LOGGER = LogUtil.GetLogger();

        static MCOS()
        {
            _slock = new();
            IsLoaded = false;
        }

        private MCOS(MinecraftInstance minecraftInstance) : base(LogUtil.GetLogger)
        {
            MinecraftInstance = minecraftInstance ?? throw new ArgumentNullException(nameof(minecraftInstance));
            TaskManager = new();
            ApplicationManager = new();
            ScreenManager = new();
            ProcessManager = new();
            FormManager = new();
            InteractionManager = new();

            FrameCount = 0;
            LagFrameCount = 0;
            FrameMinTime = TimeSpan.FromMilliseconds(50);
            PreviousFrameTime = TimeSpan.Zero;
            NextFrameTime = PreviousFrameTime + FrameMinTime;
            SystemTimer = new();
            ServicesAppID = ConfigManager.SystemConfig.ServicesAppID;
            StartupChecklist = ConfigManager.SystemConfig.StartupChecklist;

            _stopwatch = new();
        }

        private static readonly object _slock;

        public static bool IsLoaded { get; private set; }

        public static MCOS Instance
        {
            get
            {
                if (_Instance is null)
                    throw new InvalidOperationException("实例未加载");
                return _Instance;
            }
        }
        private static MCOS? _Instance;

        private readonly Stopwatch _stopwatch;

        public TimeSpan SystemRunningTime => _stopwatch.Elapsed;

        public TimeSpan FrameMinTime { get; set; }

        public TimeSpan PreviousFrameTime { get; private set; }

        public TimeSpan NextFrameTime { get; private set; }

        public int FrameCount { get; private set; }

        public int LagFrameCount { get; private set; }

        public SystemTimer SystemTimer { get; }

        public MinecraftInstance MinecraftInstance { get; }

        public TaskManager TaskManager { get; }

        public ApplicationManager ApplicationManager { get; }

        public ScreenManager ScreenManager { get; }

        public ProcessManager ProcessManager { get; }

        public FormManager FormManager { get; }

        public InteractionManager InteractionManager { get; }

        public string ServicesAppID { get; }

        public IReadOnlyList<string> StartupChecklist { get; }

        public static MCOS LoadInstance(MinecraftInstance minecraftInstance)
        {
            if (minecraftInstance is null)
                throw new ArgumentNullException(nameof(minecraftInstance));

            lock (_slock)
            {
                if (_Instance is not null)
                    throw new InvalidOperationException("试图重复加载单例实例");

                _Instance ??= new(minecraftInstance);
                IsLoaded = true;
                return _Instance;
            }
        }

        private void Initialize()
        {
            LOGGER.Info("正在等待Minecraft服务器启动...");
            MinecraftInstance.WaitForConnection();
            MinecraftInstance.Start();
            Thread.Sleep(1000);
            LOGGER.Info("成功连接到Minecraft服务器");

            LOGGER.Info("开始初始化");

            TaskManager.Initialize();
            ScreenManager.Initialize();
            InteractionManager.Initialize();
            MinecraftInstance.CommandSender.SendCommand($"scoreboard objectives add {ConfigManager.ScreenConfig.RightClickObjective} minecraft.used:minecraft.snowball");

            LOGGER.Info("初始化完成");
        }

        protected override void Run()
        {
            LOGGER.Info("系统已开始运行");
            Initialize();
            _stopwatch.Start();

            run:

#if TryCatch
            try
            {
#endif
                while (IsRuning)
                {
                    PreviousFrameTime = SystemRunningTime;
                    NextFrameTime = PreviousFrameTime + FrameMinTime;
                    FrameCount++;

                    HandleScreenScheduling();
                    HandleProcessScheduling();
                    HandleFormScheduling();
                    HandleInteractionScheduling();

                    if (TaskManager.IsCompletedCurrentMainTask)
                    {
                        HandleScreenInput();
                        HandleScreenBuild();
                    }
                    else
                    {
                        TaskManager.AddTask(() => HandleScreenInput());
                        TaskManager.AddTask(() => HandleScreenBuild());
                        LagFrameCount++;
                    }

                    HandleBeforeFrame();
                    HandleUIRendering(out var frames);
                    HandleScreenOutput(frames);
                    HandleAfterFrame();
                    HandleSystemInterrupt();

                    SystemTimer.TotalTime.Add(SystemRunningTime - PreviousFrameTime);
                }
#if TryCatch
            }
            catch (Exception ex)
            {
                bool connection = MinecraftInstance.TestConnection();

                if (!connection)
                {
                    LOGGER.Fatal("系统运行时遇到意外错误，并且无法继续连接到Minecraft服务器，系统即将终止运行", ex);
                }
                else if (ConfigManager.SystemConfig.CrashAutoRestart)
                {
                    foreach (var context in ScreenManager.Items.Values)
                    {
                        context.RestartScreen();
                    }
                    LOGGER.Error("系统运行时遇到意外错误，已启用自动重启，系统即将在3秒后重启", ex);
                    for (int i = 3; i >= 1; i--)
                    {
                        LOGGER.Info($"将在{i}秒后自动重启...");
                        Thread.Sleep(1000);
                    }
                    TaskManager.Clear();
                    LOGGER.Info("开始重启...");
                    goto run;
                }
                else
                {
                    LOGGER.Fatal("系统运行时遇到意外错误，未启用自动重启，系统即将终止运行", ex);
                }
            }
#endif

            _stopwatch.Stop();
            LOGGER.Info("系统已终止运行");
        }

        protected override void DisposeUnmanaged()
        {
            LOGGER.Info("开始释放非托管资源");

            bool connection = MinecraftInstance.TestConnection();
            if (!connection)
            {
                LOGGER.Warn("无法继续连接到Minecraft服务器，因此无法释放托管在Minecraft中的资源");
                return;
            }

            foreach (var context in ScreenManager.Items.Values)
            {
                context.Screen.Fill();
                context.Screen.UnloadScreenChunks();
            }

            foreach (var interaction in InteractionManager.Items.Values)
            {
                interaction.Dispose();
            }

            LOGGER.Info("非托管资源释放完成");
        }

        private TimeSpan HandleScreenScheduling()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ScreenManager.ScreenScheduling();

            stopwatch.Stop();
            SystemTimer.ScreenScheduling.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleProcessScheduling()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ProcessManager.ProcessScheduling();

            stopwatch.Stop();
            SystemTimer.ProcessScheduling.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleInteractionScheduling()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            InteractionManager.InteractionScheduling();

            stopwatch.Stop();
            SystemTimer.InteractionScheduling.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleFormScheduling()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            FormManager.FormScheduling();

            stopwatch.Stop();
            SystemTimer.FormScheduling.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleScreenInput()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ScreenManager.HandleAllScreenInput();

            stopwatch.Stop();
            SystemTimer.ScreenInput.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        public TimeSpan HandleAfterFrame()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ScreenManager.HandleAllAfterFrame();

            stopwatch.Stop();
            SystemTimer.HandleAfterFrame.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleUIRendering(out Dictionary<int, ArrayFrame> frames)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ScreenManager.HandleAllUIRendering(out frames);

            stopwatch.Stop();
            SystemTimer.UIRendering.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleScreenOutput(Dictionary<int, ArrayFrame> frames)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            TaskManager.ResetCurrentMainTask();
            Task task = ScreenManager.HandleAllScreenOutputAsync(frames);
            TaskManager.SetCurrentMainTask(task);
            TaskManager.WaitForPreviousMainTask();

            stopwatch.Stop();
            SystemTimer.ScreenOutput.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleBeforeFrame()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ScreenManager.HandleAllBeforeFrame();

            stopwatch.Stop();
            SystemTimer.HandleBeforeFrame.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleScreenBuild()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            ScreenManager.ScreenBuilder.Handle();

            stopwatch.Stop();
            SystemTimer.ScreenBuild.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        private TimeSpan HandleSystemInterrupt()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            int time = (int)((NextFrameTime - SystemRunningTime).TotalMilliseconds - 10);
            if (time > 0)
                Thread.Sleep(time);
            while (SystemRunningTime < NextFrameTime)
                Thread.Yield();

            stopwatch.Stop();
            SystemTimer.SystemInterrupt.Add(stopwatch.Elapsed);
            return stopwatch.Elapsed;
        }

        public ScreenContext? ScreenContextOf(IForm form)
        {
            if (form is null)
                throw new ArgumentNullException(nameof(form));

            foreach (var context in ScreenManager.Items.Values)
                if (context.RootForm == form || context.RootForm.ContainsForm(form))
                    return context;

            return null;
        }

        public ProcessContext? ProcessOf(ApplicationBase application)
        {
            if (application is null)
                throw new ArgumentNullException(nameof(application));

            foreach (var process in ProcessManager.Items.Values)
                if (application == process.Application)
                    return process;

            return null;
        }

        public ProcessContext? ProcessOf(IForm form)
        {
            if (form is null)
                throw new ArgumentNullException(nameof(form));

            FormContext? context = FormContextOf(form);
            if (context is null)
                return null;

            return ProcessOf(context.Application);
        }

        public FormContext? FormContextOf(IForm form)
        {
            if (form is null)
                throw new ArgumentNullException(nameof(form));

            foreach (var context in FormManager.Items.Values.ToArray())
                if (form == context.Form)
                    return context;

            return null;
        }

        public ProcessContext RunApplication(ApplicationInfo appInfo, IForm? initiator = null)
        {
            if (appInfo is null)
                throw new ArgumentNullException(nameof(appInfo));

            return ProcessManager.Items.Add(appInfo, initiator).StartProcess();
        }

        public ProcessContext RunApplication(ApplicationInfo appInfo, string[] args, IForm? initiator = null)
        {
            if (appInfo is null)
                throw new ArgumentNullException(nameof(appInfo));
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            return ProcessManager.Items.Add(appInfo, args, initiator).StartProcess();
        }

        public ProcessContext RunApplication(string appID, string[] args, IForm? initiator = null)
        {
            if (string.IsNullOrEmpty(appID))
                throw new ArgumentException($"“{nameof(appID)}”不能为 null 或空。", nameof(appID));

            return ProcessManager.Items.Add(ApplicationManager.Items[appID], args, initiator).StartProcess();
        }

        public ProcessContext RunApplication(string appID, IForm? initiator = null)
        {
            if (string.IsNullOrEmpty(appID))
                throw new ArgumentException($"“{nameof(appID)}”不能为 null 或空。", nameof(appID));

            return ProcessManager.Items.Add(ApplicationManager.Items[appID], initiator).StartProcess();
        }

        public ScreenContext LoadScreen(Screen screen)
        {
            if (screen is null)
                throw new ArgumentNullException(nameof(screen));

            return ScreenManager.Items.Add(screen).LoadScreen();
        }

        internal ProcessContext RunServicesApp()
        {
            if (!ApplicationManager.Items[ServicesAppID].TypeObject.IsSubclassOf(typeof(ServicesApplicationBase)))
                throw new InvalidOperationException("无效的ServicesAppID");

            return RunApplication(ServicesAppID);
        }

        internal void RunStartupChecklist(IRootForm rootForm)
        {
            foreach (var id in StartupChecklist)
                RunApplication(ApplicationManager.Items[id], rootForm);
        }
    }
}
