using Dusk;

namespace YandereCuteamena;
public class CuteamenaHandler : ContentHandler<CuteamenaHandler>
{
	public class CuteamenaBundle(DuskMod mod, string filePath) : AssetBundleLoader<CuteamenaBundle>(mod, filePath)
	{
    }

	public CuteamenaBundle? Cuteamena = null;
	public CuteamenaHandler(DuskMod mod) : base(mod)
	{
		RegisterContent("cuteaassets", out Cuteamena);
	}
}