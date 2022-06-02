using BepInEx;
using BepInEx.Configuration;
using UnhollowerRuntimeLib;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FoundryCommands
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class FoundryCommandsLoader : BepInEx.IL2CPP.BasePlugin
    {
        public const string
            MODNAME = "FoundryCommands",
            AUTHOR = "erkle64",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.0.0";

        public static BepInEx.Logging.ManualLogSource log;

        public FoundryCommandsLoader()
        {
            log = Log;
        }

        public override void Load()
        {
            log.LogMessage("Registering PluginComponent in Il2Cpp");

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<PluginComponent>();

                var go = new GameObject("Erkle64_FoundryCommands_PluginObject");
                go.AddComponent<PluginComponent>();
                Object.DontDestroyOnLoad(go);
            }
            catch
            {
                log.LogError("FAILED to Register Il2Cpp Type: PluginComponent!");
            }

            try
            {
                var harmony = new Harmony(GUID);

                var original = AccessTools.Method(typeof(ChatFrame), "onReturnCB");
                var pre = AccessTools.Method(typeof(PluginComponent), "processChatEvent");
                harmony.Patch(original, prefix: new HarmonyMethod(pre));
            }
            catch
            {
                log.LogError("Harmony - FAILED to Apply Patch's!");
            }
        }
    }
}