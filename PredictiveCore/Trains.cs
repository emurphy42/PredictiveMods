using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PredictiveCore
{
	public static class Trains
	{
		public struct Prediction
		{
			public SDate date;
			public int time;
		}

		// Whether this module should be available for player use.
		public static bool IsAvailable =>
			SDate.Now ().DaysSinceStart >= 31 &&
			(Utilities.Config.InaccuratePredictions ||
				(Utilities.SupportedVersion &&
				!Utilities.Helper.ModRegistry.IsLoaded ("AairTheGreat.BetterTrainLoot")));

		// Lists the next several trains to arrive on or after the given date,
		// up to the given limit.
		public static List<Prediction> ListNextTrainsFromDate (SDate fromDate, uint limit)
		{
			Utilities.CheckWorldReady ();
			if (!IsAvailable)
				throw new UnavailableException ("trains");

			// Logic from StardewValley.Locations.Railroad.DayUpdate()
			// as implemented in Stardew Predictor by MouseyPounds.

			List<Prediction> predictions = new ();

			// Start from today, or Summer 3 Year 1, whichever is later (in case BypassFriendships flag is set)
			for (int days = Math.Max (fromDate.DaysSinceStart, 31);
				predictions.Count < limit &&
					days < fromDate.DaysSinceStart + Utilities.MaxHorizon;
				++days)
			{
                // Base game's Railroad.DayUpdate() uses Utility.CreateDaySaveRandom() which depends on the current date
                // but we need to emulate what this would do in the past/future
                Random rng = Utility.CreateRandom(days, Game1.uniqueIDForThisGame / 2);

                if (!(rng.NextDouble () < 0.2))
					continue;

                int time = rng.Next (900, 1800);
				time -= time % 10;
				if (time % 100 >= 60)
					continue;

                var dayOfPrediction = SDate.FromDaysSinceStart(days);

                // No train on the first day after loading the game.
                if (dayOfPrediction == Utilities.LoadDate)
					continue;

                // No train on festival days.
                if (Utility.isFestivalDay(day: dayOfPrediction.Day, season: dayOfPrediction.Season))
					continue;

                predictions.Add (new Prediction { date = dayOfPrediction, time = time });
			}

			return predictions;
		}

		internal static void Initialize (bool addConsoleCommands)
		{
			if (!addConsoleCommands)
				return;
			Utilities.Helper.ConsoleCommands.Add ("predict_trains",
				"Predicts the next several trains to arrive on or after a given date, or today by default.\n\nUsage: predict_trains [<limit> [<year> <season> <day>]]\n- limit: number of trains to predict (default 20)\n- year: the target year (a number starting from 1).\n- season: the target season (one of 'spring', 'summer', 'fall', 'winter').\n- day: the target day (a number from 1 to 28).",
				(_command, args) => ConsoleCommand (args.ToList ()));
		}

		private static void ConsoleCommand (List<string> args)
		{
			try
			{
				uint limit = 20;
				if (args.Count > 0)
				{
					if (!uint.TryParse (args[0], out limit) || limit < 1)
						throw new ArgumentException ($"Invalid limit '{args[0]}', must be a number 1 or higher.");
					args.RemoveAt (0);
				}
				SDate date = Utilities.ArgsToSDate (args);

				List<Prediction> predictions = ListNextTrainsFromDate (date, limit);
				Utilities.Monitor.Log ($"Next {limit} train(s) arriving on or after {date}:",
					LogLevel.Info);
				foreach (Prediction prediction in predictions)
				{
					Utilities.Monitor.Log ($"- {prediction.date} at {Game1.getTimeOfDayString (prediction.time)}",
						LogLevel.Info);
				}
			}
			catch (Exception e)
			{
				Utilities.Monitor.Log (e.Message, LogLevel.Error);
			}
		}
	}
}
