
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace HardStyle;

[HarmonyPatch]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; private set; } = null!;

    internal static Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} loaded! Yippee!!!");
        gameObject.hideFlags = HideFlags.DontSaveInEditor;

        harmony.PatchAll();
    }
}