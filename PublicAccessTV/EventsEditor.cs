using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PublicAccessTV
{
	internal class EventsEditor
	{
		protected static IModHelper Helper => ModEntry.Instance.Helper;
		protected static IMonitor Monitor => ModEntry.Instance.Monitor;
		protected static ModConfig Config => ModConfig.Instance;

        public void OnAssetRequested(object sender, AssetRequestedEventArgs e)
		{
            if (Config.BypassFriendships)
            {
                return;
            }

            if (e.Name.IsEquivalentTo($"Data\\Events\\{GarbageChannel.EventMap}"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    applyEvents("garbage", data, GarbageChannel.Events);
                });
            }

            if (e.Name.IsEquivalentTo($"Data\\Events\\{TrainsChannel.EventMap}"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    applyEvents("trains", data, TrainsChannel.Events);
                });
            }
        }

		private void applyEvents (string module, IDictionary<string, string> to,
			IDictionary<string, string> from)
		{
			foreach (string key in from.Keys.ToList ())
			{
				to[key] = from[key] = Regex.Replace (from[key], @"\{\{([^}]+)\}\}",
					(match) => Helper.Translation.Get ($"{module}.event.{match.Groups[1]}"));
			}
		}
	}
}
