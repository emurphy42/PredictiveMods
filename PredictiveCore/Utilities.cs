using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PredictiveCore
{
	public class UnavailableException : InvalidOperationException
	{
		public UnavailableException (string topic)
			: base ($"Predictions about {topic} are not available.")
		{ }
	}

	public class WorldUnreadyException : InvalidOperationException
	{
		public WorldUnreadyException ()
			: base ("The world is not ready.")
		{ }
	}

	public interface IConfig
	{
#pragma warning disable IDE1006

		bool InaccuratePredictions { get; }

#pragma warning restore IDE1006
	}

	public delegate IConfig ConfigProvider ();

	public static class Utilities
	{
		public static bool SupportedVersion =>
			new SemanticVersion (Game1.version).IsBetween ("1.6.0", "1.7.0");

		// Parses a list of three console arguments (year, season, day) as an
		// SDate. If the list is empty, returns the current date.
		public static SDate ArgsToSDate (List<string> args)
		{
			switch (args.Count)
			{
			case 0:
				return SDate.Now ();
			case 3:
				break;
			default:
				throw new ArgumentException ("Wrong number of arguments.");
			}

			if (!int.TryParse (args[0], out int year) || year < 1)
			{
				throw new ArgumentException ($"Invalid year '{args[0]}', must be a number 1 or higher.");
			}

			string season = args[1];
			if (Utility.getSeasonNumber (season) == -1)
			{
				throw new ArgumentException ($"Invalid season '{args[1]}', must be 'spring', 'summer', 'fall' or 'winter'.");
			}

			if (!int.TryParse (args[2], out int day) || day < 1 || day > 28)
			{
				throw new ArgumentException ($"Invalid day '{args[2]}', must be a number from 1 to 28.");
			}

			return new SDate (year, season, day);
		}

		// Returns the first day of the next season after a given date.
		public static SDate GetNextSeasonStart (SDate date)
		{
			return date.Season switch
			{
				Season.Spring => new SDate (1, "summer", date.Year),
				Season.Summer => new SDate (1, "fall", date.Year),
				Season.Fall => new SDate (1, "winter", date.Year),
				Season.Winter => new SDate (1, "spring", date.Year + 1),
				_ => throw new ArgumentOutOfRangeException ("invalid season"),
			};
		}

		private static readonly PerScreen<SDate> LoadDate_ = new ();
		public static SDate LoadDate => LoadDate_.Value;

		// Returns the CultureInfo for the current game language.
		public static CultureInfo GetCurrentCulture ()
		{
			string langCode = LocalizedContentManager.LanguageCodeString
                (Game1.content.GetCurrentLanguage ());
			return new CultureInfo (langCode);
		}

		// Returns the localized name of the day of the week for a given date.
		public static string GetLocalizedDayOfWeek (SDate date,
			bool shortName = false)
		{
			if (shortName)
			{
				return Game1.shortDayDisplayNameFromDayOfSeason (date.Day);
			}
			else
			{
				// February 1999 was a Stardew month: 28 days starting on Monday
				return new DateTime (1999, 2, date.Day).ToString ("dddd",
					GetCurrentCulture ());
			}
		}

		// Limits prediction calculations to 50 years from start date.
		internal const int MaxHorizon = 28 * 4 * 50;

		internal static IMonitor Monitor;
		internal static IModHelper Helper;

		private static ConfigProvider ConfigProvider;
		internal static IConfig Config => ConfigProvider ();

		public static void Initialize (IMod mod, ConfigProvider config)
		{
			if (Monitor != null)
				return;
			Monitor = mod.Monitor;
			Helper = mod.Helper;
			ConfigProvider = config;

			// Check for a game version within range.
			if (!SupportedVersion)
			{
				Monitor.Log ($"This version of {mod.ModManifest.Name} was not designed for game version {Game1.version}. Predictions will be inaccurate.", LogLevel.Alert);
			}

			// If multiple mods are consuming PredictiveCore, only add the
			// console commands in one of them (arbitrarily, PublicAccessTV).
			bool addConsoleCommands =
				mod.ModManifest.UniqueID == "kdau.PublicAccessTV" ||
				!Helper.ModRegistry.IsLoaded ("kdau.PublicAccessTV");

			// Initialize prediction modules.
			Enchantments.Initialize (addConsoleCommands);
			Garbage.Initialize (addConsoleCommands);
			Geodes.Initialize (addConsoleCommands);
			Mining.Initialize (addConsoleCommands);
			Movies.Initialize (addConsoleCommands);
			NightEvents.Initialize (addConsoleCommands);
			Trains.Initialize (addConsoleCommands);

			// Listen for game events.
			Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
		}

		public static void CheckWorldReady ()
		{
			if (!Context.IsWorldReady)
				throw new WorldUnreadyException ();
		}

		private static void OnSaveLoaded (object _sender, SaveLoadedEventArgs _e)
		{
			LoadDate_.Value = SDate.Now ();
		}
	}
}
