using PredictiveCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace PublicAccessTV
{
	internal class MailEditor
	{
		protected static IModHelper Helper => ModEntry.Instance.Helper;
		protected static IMonitor Monitor => ModEntry.Instance.Monitor;

        public void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Data\\mail"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    string letter = Helper.Translation.Get("mining.letter.content") +
                        "[#]" + Helper.Translation.Get("mining.letter.title");
                    data["kdau.PublicAccessTV.mining"] = letter;
                });
            }
        }
	}
}
