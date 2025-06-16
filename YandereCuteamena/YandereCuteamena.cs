using BepInEx;
using BepInEx.Logging;
using System.Reflection;
using UnityEngine;
using CodeRebirthLib;
using CodeRebirthLib.Extensions;

namespace YandereCuteamena;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class YandereCuteamena : BaseUnityPlugin
{
	internal new static ManualLogSource Logger { get; private set; }

	public static CRMod Mod { get; private set; }
	
	private void Awake()
	{
		Logger = base.Logger;

		NetcodePatcher();

		AssetBundle mainBundle = CRLib.LoadBundle(Assembly.GetExecutingAssembly(), "yanderecuteamenaassets");
		Mod = CRLib.RegisterMod(this, mainBundle);
		Mod.RegisterContentHandlers();

		Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
	}

	private void NetcodePatcher()
	{
		var types = Assembly.GetExecutingAssembly().GetLoadableTypes();
		foreach (var type in types)
		{
			var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			foreach (var method in methods)
			{
				var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
				if (attributes.Length <= 0)
					continue;
				method.Invoke(null, null);
			}
		}
	}
}