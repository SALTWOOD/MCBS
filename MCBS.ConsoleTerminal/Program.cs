﻿#define TryCatch

using FFMediaToolkit;
using log4net.Core;
using MCBS.BlockForms.Utility;
using MCBS.Config;
using MCBS.Logging;
using MCBS.SystemApplications;
using QuanLib.Minecraft;
using QuanLib.Minecraft.API;
using QuanLib.Minecraft.API.Instance;
using QuanLib.Minecraft.API.Packet;
using QuanLib.Minecraft.Instance;
using QuanLib.Minecraft.ResourcePack.Language;
using System.Net;
using System.Text;

namespace MCBS.ConsoleTerminal
{
    public static class Program
    {
        private static readonly LogImpl LOGGER;

        static Program()
        {
            Thread.CurrentThread.Name = "Main Thread";
            ConfigManager.CreateIfNotExists();
            LOGGER = LogUtil.GetLogger();
            Terminal = new();
        }

        public static Terminal Terminal { get; }

        private static void Main(string[] args)
        {
            LOGGER.Info("MCBS已启动，欢迎使用！");
            Terminal.Start("Terminal Thread");

#if TryCatch
            try
            {
#endif
            ConfigManager.LoadAll();
            MR.LoadAll();
            SR.LoadAll();
            TextureManager.LoadInstance();
            FFmpegResourcesLoader.LoadAll();
#if TryCatch
            }
            catch (Exception ex)
            {
                LOGGER.Fatal("无法完成资源文件的加载", ex);
                Exit();
                return;
            }
#endif

            MinecraftInstance minecraftInstance;

#if TryCatch
            try
            {
#endif
            MinecraftConfig config = ConfigManager.MinecraftConfig;
            switch (config.InstanceType)
            {
                case InstanceTypes.CLIENT:
                    if (config.CommunicationMode == CommunicationModes.MCAPI)
                        minecraftInstance = new McapiMinecraftClient(config.MinecraftPath, config.ServerAddress, config.McapiPort, config.McapiPassword, LogUtil.GetLogger);
                    else
                        throw new InvalidOperationException();
                    break;
                case InstanceTypes.SERVER:
                    minecraftInstance = config.CommunicationMode switch
                    {
                        CommunicationModes.RCON => new RconMinecraftServer(config.MinecraftPath, config.ServerAddress, LogUtil.GetLogger),
                        CommunicationModes.CONSOLE => new ConsoleMinecraftServer(config.MinecraftPath, config.ServerAddress, new GenericServerLaunchArguments(config.JavaPath, config.LaunchArguments), LogUtil.GetLogger),
                        CommunicationModes.HYBRID => new HybridMinecraftServer(config.MinecraftPath, config.ServerAddress, new GenericServerLaunchArguments(config.JavaPath, config.LaunchArguments), LogUtil.GetLogger),
                        CommunicationModes.MCAPI => new McapiMinecraftServer(config.MinecraftPath, config.ServerAddress, config.McapiPort, config.McapiPassword, LogUtil.GetLogger),
                        _ => throw new InvalidOperationException(),
                    };
                    break;
                default:
                    throw new InvalidOperationException();
            }
#if TryCatch
            }
            catch (Exception ex)
            {
                LOGGER.Fatal("无法绑定到Minecraft实例", ex);
                Exit();
                return;
            }
#endif

            MCOS mcos = MCOS.LoadInstance(minecraftInstance);
            AppComponentLoader.LoadAll(mcos);

            mcos.Start("System Thread");
            mcos.WaitForStop();

            Exit();
            return;
        }

        public static void Exit()
        {
            Task task = Terminal.WaitForStopAsync();
            Terminal.Stop();
            task.Wait();

            for (int i = 10; i >= 1; i--)
            {
                LOGGER.Info($"MCBS将在{i}秒后退出，按下回车键立即退出");
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                        break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

            LOGGER.Info("MCBS已退出，感谢使用！");
        }
    }
}
