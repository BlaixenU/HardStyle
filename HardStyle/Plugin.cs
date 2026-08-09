using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace HardStyle;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static string AssemblyPath => Path.GetDirectoryName(typeof(Plugin).Assembly.Location);

    public static string ModDir = Path.GetFullPath(Path.Combine(AssemblyPath, @"../"));

    internal static new ManualLogSource Logger { get; private set; } = null!;

    private static Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    public static Config config = new Config(MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded! Yippee!!!");
        gameObject.hideFlags = HideFlags.DontSaveInEditor;

        harmony.PatchAll();
    }

}