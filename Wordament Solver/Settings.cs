using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Drawing;

namespace Wordament
{
	public static class Settings
	{
		static Settings()
		{
			var properties = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static);

			foreach (var property in properties)
			{
				var settingName = IdentifierToXmlName(property.Name);
				
				var settingValue = ConfigurationManager.AppSettings[settingName];

				if (property.PropertyType == typeof(int))
				{
					property.SetValue(null, int.Parse(settingValue));
				}
				else if (property.PropertyType == typeof(Color))
				{
					if (settingValue[0] == '#')
					{
						property.SetValue(null, ColorTranslator.FromHtml(settingValue));
					}
					else if (settingValue.Contains(','))
					{
						var components = settingValue.Split(new char[] { ',' }, StringSplitOptions.None);
						
						var color = Color.FromArgb(
							int.Parse(components[0].Trim()),
							int.Parse(components[1].Trim()),
							int.Parse(components[2].Trim()));

						property.SetValue(null, color);
					}
				}
				else
				{
					property.SetValue(null, settingValue);
				}
			}
		}

		private static string IdentifierToXmlName(string identifier)
		{
			var sb = new StringBuilder();
			var wasLower = false;

			foreach (var c in identifier)
			{
				var isUpper = char.IsUpper(c);

				if (isUpper && wasLower)
				{
					sb.Append("-");
				}
				sb.Append(c.ToString().ToLower());

				wasLower = !isUpper;
			}

			return sb.ToString();
		}

		public static int ValidTileWidth { get; private set; }

		public static int ValidTileHeight { get; private set; }

		public static string GameUrl { get; private set; }

		public static int TileScoreWidth { get; private set; }

		public static int TileScoreHeight { get; private set; }

		public static Color GuessingColor { get; private set; }

		public static Color TimesUpColor { get; private set; }

		public static Color LobbyBackgroundColor { get; private set; }

		public static Color NormalTileColor { get; private set; }

		public static Color ValidWordTileColor { get; private set; }

		public static Color InvalidWordTileColor { get; private set; }

		public static int PostWordSleepTime { get; private set; }

		public static int PostTileSleepTime { get; private set; }

		public static string TesseractFileName { get; private set; }

		public static string TesseractUrl { get; private set; }
	}
}
