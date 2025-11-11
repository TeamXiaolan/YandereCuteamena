using BepInEx;
using BepInEx.Logging;
using Dawn.Utils;
using Dusk;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace YandereCuteamena;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class YandereCuteamena : BaseUnityPlugin
{
	internal new static ManualLogSource Logger { get; private set; }
    internal static readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

	public static DuskMod Mod { get; private set; }
    internal static MainAssets Assets { get; private set; } = null!;
    internal class MainAssets(AssetBundle bundle) : AssetBundleLoader<MainAssets>(bundle)
    {
        [LoadFromBundle("CuteamenaUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; } = null!;
    }

	private void Awake()
	{
		Logger = base.Logger;

        _harmony.PatchAll(typeof(StartOfRoundPatch));

        AssetBundle mainBundle = AssetBundleUtils.LoadBundle(Assembly.GetExecutingAssembly(), "yanderecuteamenaassets");
        Assets = new MainAssets(mainBundle);
        Mod = DuskMod.RegisterMod(this, mainBundle);

		Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
	}
}