using CodeRebirthLib;
using CodeRebirthLib.AssetManagement;
using CodeRebirthLib.ContentManagement;

namespace YandereCuteamena;

public class CuteamenaHandler : ContentHandler<CuteamenaHandler>
{
	public class CuteamenaBundle(CRMod mod, string filePath) : AssetBundleLoader<CuteamenaBundle>(mod, filePath)
	{
    }

    public CuteamenaHandler(CRMod mod) : base(mod)
	{
		if (TryLoadContentBundle("cuteaassets", out CuteamenaBundle? assets))
		{
			LoadAllContent(assets!);
		}
	}
}