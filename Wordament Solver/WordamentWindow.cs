using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Wordament
{
	public class WordamentWindow
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, out System.Drawing.Rectangle rect);

		public static Process FindProcess()
		{
			return Process.GetProcesses().FirstOrDefault(p => p.MainWindowTitle.Contains("Wordament - Xbox.com"));
		}

		private delegate bool InRectFunction(int x, int y);
		private delegate bool FindNextPixelFunction(
			int prevX, int prevY,
			int currX, int currY,
			out int nextX, out int nextY);

		public static Rectangle[] GetTileRects(Bitmap screenShot)
		{
			var screen = new FastBitmapWrapper(screenShot);

			var rects = new List<Rectangle>();

			InRectFunction inRect = delegate(int x, int y)
			{
				foreach (var rect in rects)
				{
					if ((x >= rect.Left) && (x <= rect.Right) &&
						(y >= rect.Top) && (y <= rect.Bottom))
						return true;
				}
				return false;
			};

			var searchOffsets = new List<Point> {
				new Point(-1, -1),
				new Point(0, -1),
				new Point(1, -1),
				new Point(1, 0),
				new Point(1, 1),
				new Point(0, 1),
				new Point(-1, 1),
				new Point(-1, 0)
			};

			FindNextPixelFunction findNextPixel = delegate(
				int prevX, int prevY,
				int currX, int currY,
				out int nextX, out int nextY)
			{
				nextX = -1;
				nextY = -1;

				Point prevOffset = new Point(prevX - currX, prevY - currY);
				int prevOffsetIndex = searchOffsets.IndexOf(prevOffset);

				List<Point> offsets = new List<Point>(searchOffsets);

				while (prevOffsetIndex >= 0)
				{
					var p = offsets[0];
					offsets.RemoveAt(0);
					offsets.Add(p);
					prevOffsetIndex--;
				}

				for (var index = 0; index < offsets.Count; index++)
				{
					var offset = offsets[index];

					nextX = currX + offset.X;
					nextY = currY + offset.Y;

					if ((nextX < 0) || (nextX >= screen.Width) || (nextY < 0) || (nextY >= screen.Height))
						continue;

					if (screen.GetPixel(nextX, nextY) == Settings.NormalTileColor)
						return true;
				}

				return false;
			};

			for (var y = 0; y < screen.Height; y++)
			{
				for (var x = 0; x < screen.Width; x++)
				{
					if (inRect(x, y))
						continue;

					if (screen.GetPixel(x, y) == Settings.NormalTileColor)
					{
						int tileLeft = x;
						int tileRight = x;
						int tileTop = y;
						int tileBottom = y;

						int prevX = x - 1;
						int prevY = y;
						int currX = x;
						int currY = y;
						int nextX;
						int nextY;

						while (findNextPixel(prevX, prevY, currX, currY, out nextX, out nextY))
						{
							if ((nextX == x) & (nextY == y))
							{
								// We've looped right back to the start
								break;
							}

							tileLeft = Math.Min(tileLeft, nextX);
							tileRight = Math.Max(tileRight, nextX);
							tileTop = Math.Min(tileTop, nextY);
							tileBottom = Math.Max(tileBottom, nextY);

							prevX = currX;
							prevY = currY;
							currX = nextX;
							currY = nextY;
						}

						Rectangle tileRect = new Rectangle(tileLeft, tileTop, tileRight - tileLeft, tileBottom - tileTop);
						if ((tileRect.Width > 0) && (tileRect.Height > 0))
							rects.Add(tileRect);
					}
				}
			}

			return rects.ToArray();
		}

		public static void TakeScreenshot(Bitmap bmp, Rectangle rect)
		{
			using (var graphics = System.Drawing.Graphics.FromImage(bmp))
			{
				// Copy from screen into our bmp
				graphics.CopyFromScreen(
					new System.Drawing.Point(rect.Left, rect.Top),
					new System.Drawing.Point(0, 0),
					new System.Drawing.Size(rect.Width, rect.Height));
			}
		}

		public static Color GetPixelAt(int screenX, int screenY)
		{
			using (var bmp = new Bitmap(1, 1))
			{
				using (var graphics = Graphics.FromImage(bmp))
				{
					graphics.CopyFromScreen(screenX, screenY, 0, 0, new Size(1, 1));
					return bmp.GetPixel(0, 0);
				}
			}
		}

		public static bool AccusedOfGuessing(Rectangle windowBounds, Rectangle[] tileRects)
		{
			if (tileRects.Length != 16)
				throw new ArgumentException("TileRects argument must be an ordered array of 16 tiles");

			var tileRect = tileRects[4];

			var testPoint = new Point(
				windowBounds.Left + tileRect.Left + 2,
				windowBounds.Top + tileRect.Bottom + 2);

			var color = GetPixelAt(testPoint.X, testPoint.Y);

			return (color == Color.Red);
		}

		private static int GetcolorDifference(Color color1, Color color2)
		{
			return
				Math.Abs(color1.R - color2.R) +
				Math.Abs(color1.G - color2.G) +
				Math.Abs(color1.B - color2.B);
		}

		public static GameState GetGameState(
			Rectangle windowBounds,
			Rectangle[] tileRects)
		{
			return GetGameState(windowBounds, tileRects, -1, "");
		}

		public static GameState GetGameState(
			Rectangle windowBounds,
			Rectangle[] tileRects,
			int lastPlayedTileIndex,
			string lastPlayedWord)
		{
			if (tileRects.Length != 16)
				return GameState.Unknown;

			var testPoint = new Point(
				windowBounds.Left + tileRects[5].Right + 4,
				windowBounds.Top + tileRects[5].Bottom + 2);

			var color = GetPixelAt(testPoint.X, testPoint.Y);

			int colorDiff;
			
			// Check for Guessing Warning overlay
			colorDiff = GetcolorDifference(color, Settings.GuessingColor);

			if (colorDiff < 10)
			{
				testPoint = new Point(
					windowBounds.Left + tileRects[lastPlayedTileIndex].Left + 1,
					windowBounds.Top + tileRects[lastPlayedTileIndex].Top + 1);

				color = GetPixelAt(testPoint.X, testPoint.Y);

				///*
				System.IO.File.AppendAllText("_Tile colors.txt", string.Format("{0},{1},{2},{3},{4},{5},{6},,",
					lastPlayedWord,
					"GUESSING",
					testPoint.X - windowBounds.Left,
					testPoint.Y - windowBounds.Top,
					color.R, color.G, color.B) + Environment.NewLine);

				ScreenshotToFile(windowBounds, "_" + lastPlayedWord + " - GUESSING.png");
				//*/
				return GameState.GuessingWarning;
			}

			// Check for Time's Up overlay
			colorDiff = GetcolorDifference(color, Settings.TimesUpColor);
			if (colorDiff < 10)
				return GameState.TimesUp;


			// Are we in the lobby or similiar screen?
			testPoint = new Point(
				windowBounds.Left + tileRects[1].Right - 1,
				windowBounds.Top + tileRects[1].Top + 1);

			color = GetPixelAt(testPoint.X, testPoint.Y);

			colorDiff = GetcolorDifference(color, Settings.LobbyBackgroundColor);
			if (colorDiff < 10)
				return GameState.Unknown;

			// Check the color of the last played tile
			// to check for valid/invalid word
			if (lastPlayedTileIndex > -1)
			{
				testPoint = new Point(
					windowBounds.Left + tileRects[lastPlayedTileIndex].Left + 1,
					windowBounds.Top + tileRects[lastPlayedTileIndex].Top + 1);

				color = GetPixelAt(testPoint.X, testPoint.Y);

				if (color != Settings.NormalTileColor)
				{
					var validcolorDiff = GetcolorDifference(color, Settings.ValidWordTileColor);
					var invalidcolorDiff = GetcolorDifference(color, Settings.InvalidWordTileColor);

					const int validWordTollerance = 175;

					//if (validcolorDiff < invalidcolorDiff)

					if (validcolorDiff < validWordTollerance)
					{
						/*
						System.IO.File.AppendAllText("_Tile colors.txt", string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
							lastPlayedWord,
							"VALID",
							testPoint.X - windowBounds.Left,
							testPoint.Y - windowBounds.Top,
							color.R, color.G, color.B,
							validcolorDiff,
							invalidcolorDiff) + Environment.NewLine);
						//*/
						return GameState.ValidWord;
					}

					if (invalidcolorDiff < validWordTollerance)
					{
						///*
						ScreenshotToFile(windowBounds, "_" + lastPlayedWord + " - INVALID.png");
						System.IO.File.AppendAllText("_Tile colors.txt", string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
							lastPlayedWord,
							"INVALID",
							testPoint.X - windowBounds.Left,
							testPoint.Y - windowBounds.Top,
							color.R, color.G, color.B,
							validcolorDiff,
							invalidcolorDiff) + Environment.NewLine);
						//*/
						return GameState.InvalidWord;
					}///*
					else
					{
						ScreenshotToFile(windowBounds, "_" + lastPlayedWord + " - UNKNOWN.png");
						System.IO.File.AppendAllText("_Tile colors.txt", string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
							lastPlayedWord,
							"?????",
							testPoint.X - windowBounds.Left,
							testPoint.Y - windowBounds.Top,
							color.R, color.G, color.B,
							validcolorDiff,
							invalidcolorDiff) + Environment.NewLine);
					}
					//*/
				}
				else
				{
					/*
					System.IO.File.AppendAllText("Tile colors.txt", string.Format("{0},{1},{2},{3},{4},{5},{6},,",
						lastPlayedWord,
						"NORMAL",
						testPoint.X - windowBounds.Left,
						testPoint.Y - windowBounds.Top,
						color.R, color.G, color.B) + Environment.NewLine);
					//*/
				}
			}

			return GameState.InGame;
		}

		private static void ScreenshotToFile(Rectangle rect, string fileName)
		{
			using (var bmp = new Bitmap(rect.Width, rect.Height))
			{
				TakeScreenshot(bmp, rect);
				var imageFormat = fileName.ToLower().EndsWith(".bmp") ? System.Drawing.Imaging.ImageFormat.Bmp :
					System.Drawing.Imaging.ImageFormat.Png;
				bmp.Save(fileName, imageFormat);
			}
		}
	}

	public enum GameState
	{
		InGame,
		ValidWord,
		InvalidWord,
		GuessingWarning,
		TimesUp,
		Unknown
	}
}