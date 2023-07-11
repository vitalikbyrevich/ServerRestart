using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace ServerRestart
{
    [BepInProcess("valheim_server.exe")]
    [BepInPlugin(Guid, Name, Version)]
    internal class Plugin : BaseUnityPlugin
    {
        public const string Guid = "org.tristan.serverrestart";
        public const string Name = "Server Restart";
        public const string Version = "1.0.7";

        public static ConfigEntry<string> RestartTimes;
        public static ConfigEntry<string> Message1Hour;
        public static ConfigEntry<string> Message10Mins;
        public static ConfigEntry<string> Message1Min;

        private static RestartService _service;

        private void Awake()
        {
            Log.CreateInstance(Logger);

            RestartTimes = Config.Bind("Restart", "Times", "", "Restart times divied by ,");
            Message1Hour = Config.Bind("Messages", "1 hour", "Server restart in 1 hour");
            Message10Mins = Config.Bind("Messages", "10 minutes", "Server restart in 10 minutes");
            Message1Min = Config.Bind("Messages", "1 minute", "Server restart in 1 minute");
            Helper.WatchConfigFileChanges(Config, OnConfigChanged);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Guid);
        }

        private void OnConfigChanged()
        {
            Log.Message("Config reloaded");
            Config.Reload();
            if (_service != null)
                _service.ScheduleNextRestart();
        }

        [HarmonyPatch]
        class Patch
        {
            [HarmonyPostfix, HarmonyPatch(typeof(Game), nameof(Game.Start))]
            private static void Game_Start(Game __instance)
            {
                _service = __instance.gameObject.AddComponent<RestartService>();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(ZNet), nameof(ZNet.CheckForIncommingServerConnections))]
            private static bool ZNet_CheckForIncommingServerConnections()
            {
                if (_service.RestartStarted || (Game.instance != null && Game.instance.IsShuttingDown()))
                    return false;

                return true;
            }

            [HarmonyPriority(Priority.Last)]
            [HarmonyFinalizer, HarmonyPatch(typeof(Game), nameof(Game.OnApplicationQuit))]
            private static void Game_OnApplicationQuit()
            {
                Thread.Sleep(5000);
                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
