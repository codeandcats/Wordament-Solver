using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Wordament
{
	public class StatusEventArgs
	{
		public StatusEventArgs(string status)
		{
			Status = status;
		}

		public string Status { get; private set; }
	}

	public delegate void StatusEvent (object sender, StatusEventArgs args);


	public class WordamentService
	{
		private BackgroundWorker worker = new BackgroundWorker();

		public WordamentService()
		{
			Running = false;
			worker.DoWork += Work;
			worker.WorkerSupportsCancellation = true;
			worker.WorkerReportsProgress = true;
			worker.ProgressChanged += ProgressChanged;
		}

		private void ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			var status = (string)e.UserState;
			if (status == "")
			{
				if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Escape))
					worker.CancelAsync();
			}
			else if (StatusChanged != null)
			{
				var args = new StatusEventArgs(status);
				StatusChanged(this, args);
			}
		}

		public void Start()
		{
			worker.RunWorkerAsync();
			Running = true;
		}

		public void Stop()
		{
			worker.CancelAsync();
			Running = false;
		}

		public StatusEvent StatusChanged { get; set; }

		private void ReportStatus(BackgroundWorker worker, string status)
		{
			worker.ReportProgress(0, status);
		}

		private void Work(object sender, DoWorkEventArgs args)
		{
			var worker = (BackgroundWorker)sender;

			ReportStatus(worker, "Service Started");
			try
			{
				bool gameRunning = false;
				Process browser = null;
				Rectangle[] tileRects = new Rectangle[] { };
				Bitmap screenShot = null;
				Rectangle browserRect = new Rectangle();
				Rectangle gameBoardRect = new Rectangle(0, 0, 0, 0);
				var tileTexts = new string[,] {
					{ "", "", "", "" },
					{ "", "", "", "" },
					{ "", "", "", "" },
					{ "", "", "", "" }
				};
				var words = new WordSequence[] { };
				var timer = new Stopwatch();

				var dictionary = new WordList();
				dictionary.LoadFromFile("word list.txt", false);

				int wordIndex = 0;

				while (!worker.CancellationPending)
				{
					if (!gameRunning)
					{
						// Wait a bit
						System.Threading.Thread.Sleep(500);

						ReportStatus(worker, "Looking for Wordament window...");

						// Find browser process
						browser = WordamentWindow.FindProcess();

						if (browser != null)
						{
							ReportStatus(worker, "Found Wordament window");

							if (!WordamentWindow.GetWindowRect(browser.MainWindowHandle, out browserRect))
							{
								ReportStatus(worker, "Couldn't get window brounds");
								continue;
							}

							//ReportStatus(worker, "Taking screenshot");
							//timer.Start();
							screenShot = new Bitmap(browserRect.Width, browserRect.Height);
							WordamentWindow.TakeScreenshot(screenShot, browserRect);
							//ReportStatus(worker, string.Format("Done in {0:#,##0}ms", timer.ElapsedMilliseconds));

							ReportStatus(worker, "Looking for tiles...");
							//timer.Restart();
							tileRects = WordamentWindow.GetTileRects(screenShot).
								Where(r => (r.Width == Settings.ValidTileWidth) && (r.Height == Settings.ValidTileHeight)).
								OrderBy(r => r.Top).ThenBy(r => r.Left).ToArray();
							//ReportStatus(worker, string.Format("Done in {0:#,##0}ms", timer.ElapsedMilliseconds));

							// Do some tests to make sure that this is the game board we're looking at
							if (tileRects.Length == 16)
							{
								var lefts = new HashSet<int>();
								var tops = new HashSet<int>();
								var widths = new HashSet<int>();
								var heights = new HashSet<int>();

								foreach (var rect in tileRects)
								{
									lefts.Add(rect.Left);
									tops.Add(rect.Top);
									widths.Add(rect.Width);
									heights.Add(rect.Height);
								}

								gameRunning =
									(lefts.Count == 4) && (tops.Count == 4) &&
									(widths.Count == 1) && (heights.Count == 1) &&
									widths.Contains(Settings.ValidTileWidth) && heights.Contains(Settings.ValidTileHeight);
							}

							if (gameRunning)
							{
								ReportStatus(worker, "Found game tiles");

								// The game appears to have started

								// First get the size of the game board area
								gameBoardRect.Location = new Point(
									tileRects.Min(r => r.Left),
									tileRects.Min(r => r.Top));
								gameBoardRect.Size = new Size(
									tileRects.Max(r => r.Right) - gameBoardRect.Left,
									tileRects.Max(r => r.Bottom) - gameBoardRect.Top);

								// Perform OCR on each tile
								ReportStatus(worker, "Performing OCR");
								using (var tileBitmap = new Bitmap(Settings.ValidTileWidth, Settings.ValidTileHeight))
								{
									using (var graphics = Graphics.FromImage(tileBitmap))
									{
										for (var index = 0; index < tileRects.Length; index++)
										{
											var tileRect = tileRects[index];

											graphics.DrawImage(screenShot, 0, 0, tileRect, GraphicsUnit.Pixel);

											Brush tileBrush = new SolidBrush(Settings.NormalTileColor);
											graphics.FillRectangle(tileBrush, 0, 0, Settings.TileScoreWidth, Settings.TileScoreHeight);

											string tileText = Ocr.ReadWord(tileBitmap);
											tileText = tileText.Trim(' ', '-');
											
											int tileX = index % 4;
											int tileY = (int)Math.Truncate(index / 4.0f);
											tileTexts[tileX, tileY] = tileText;

											//ReportStatus(worker, string.Format("Tile[{0}, {1}] = \"{2}\"", tileX, tileY, tileText));
										}
									}
								}

								ReportStatus(worker, "Gameboard:");
								for (var y = 0; y < 4; y++)
								{
									string line = "";
									for (var x = 0; x < 4; x++)
									{
										line += " " + tileTexts[x, y];
									}
									ReportStatus(worker, line);
								}

								ReportStatus(worker, "Finding Words");
								var finder = new WordFinder();

								words = finder.FindWordSequences(dictionary, tileTexts).
									Distinct(new DistinctWordComparer()).
									OrderByDescending(w => w.Word.Length).ToArray();

								ReportStatus(worker, string.Format("Found {0} words", words.Length));

								wordIndex = 0;
								/*
								string wordMessage = words[0].Word + " = ";
								for (var index = 0; index < words[0].Tiles.Length; index++)
								{
									if (index > 0)
										wordMessage += ", ";
									wordMessage += string.Format("[{0},{1}]", words[0].Tiles[index].X, words[0].Tiles[index].Y);
								}
								ReportStatus(worker, "Word: " + wordMessage);
								*/
							}
						}
					}

					if (gameRunning)
					{
						int lastPlayedTileIndex = -1;
						string lastPlayedWord = "";

						if (wordIndex >= words.Length)
						{
							//ReportStatus(worker, "Finished playing all found words!");
							System.Threading.Thread.Sleep(1000);
						}
						else
						{
							var seq = words[wordIndex];

							//ReportStatus(worker, "Playing word: " + seq.Word);

							const int pathSmoothingPointCount = 4;

							int previousTileCenterX = -1;
							int previousTileCenterY = -1;

							for (var tileIndex = 0; tileIndex < seq.Tiles.Length; tileIndex++)
							{
								var tileLocation = seq.Tiles[tileIndex];
								int gridTileIndex = (int)(tileLocation.X + (tileLocation.Y * 4));
								var tileRect = tileRects[gridTileIndex];
								int tileCenterX = browserRect.Left + tileRect.Left + (tileRect.Width / 2);
								int tileCenterY = browserRect.Top + tileRect.Top + (tileRect.Height / 2);

								if ((pathSmoothingPointCount > 0) && (tileIndex > 0))
								{
									float stepX = (tileCenterX - previousTileCenterX) / (pathSmoothingPointCount + 1);
									float stepY = (tileCenterY - previousTileCenterY) / (pathSmoothingPointCount + 1);

									for (var pathSmoothingIndex = 0; pathSmoothingIndex < pathSmoothingPointCount; pathSmoothingIndex++)
									{
										int x = (int)(previousTileCenterX + (stepX * (pathSmoothingIndex + 1)));
										int y = (int)(previousTileCenterY + (stepY * (pathSmoothingIndex + 1)));
										MouseControl.SetMousePos(x, y);
										System.Threading.Thread.Sleep(5);
									}
								}

								MouseControl.SetMousePos(tileCenterX, tileCenterY);

								if (tileIndex == 0)
									MouseControl.LeftButtonDown(tileCenterX, tileCenterY);
								else if (tileIndex == seq.Tiles.Length - 1)
									MouseControl.LeftButtonUp(tileCenterX, tileCenterY);

								previousTileCenterX = tileCenterX;
								previousTileCenterY = tileCenterY;

								// Wait a very few ms after swiping a tile
								System.Threading.Thread.Sleep(Settings.PostTileSleepTime);
							}

							// Wait a few ms after playing a word
							System.Threading.Thread.Sleep(Settings.PostWordSleepTime);

							lastPlayedTileIndex =
								(int)(seq.Tiles[seq.Tiles.Length - 1].X +
								(4 * seq.Tiles[seq.Tiles.Length - 1].Y));
							lastPlayedWord = seq.Word;

							wordIndex++;
						}

						GameState gameState = WordamentWindow.GetGameState(browserRect, tileRects, lastPlayedTileIndex, lastPlayedWord);

						switch (gameState)
						{
							case GameState.ValidWord:
								if (lastPlayedWord != "")
									ReportStatus(worker, "Played valid word: " + lastPlayedWord);
								System.Threading.Thread.Sleep(100);
								if (wordIndex >= words.Length)
									ReportStatus(worker, "Finished playing all found words");
								break;

							case GameState.InvalidWord:
								if (lastPlayedWord != "")
									ReportStatus(worker, "Played INVALID word: " + lastPlayedWord);
								System.Threading.Thread.Sleep(100);
								if (wordIndex >= words.Length)
									ReportStatus(worker, "Finished playing all found words");
								break;

							case GameState.GuessingWarning:
								if (lastPlayedWord != "")
									ReportStatus(worker, "Played INVALID word: " + lastPlayedWord);
								ReportStatus(worker, "Accused of guessing - How rude!");
								System.Threading.Thread.Sleep(1700);
								if (wordIndex >= words.Length)
									ReportStatus(worker, "Finished playing all found words");
								break;

							case GameState.TimesUp:
								ReportStatus(worker, "The only winning move is not to play");
								System.Threading.Thread.Sleep(25000);
								gameRunning = false;
								break;

							case GameState.Unknown:
								ReportStatus(worker, "Unsure of game state - assuming game over");
								System.Threading.Thread.Sleep(3000);
								gameRunning = false;
								break;
						
							case GameState.InGame:
							default:
								// Keep playing!
								System.Threading.Thread.Sleep(100);
								break;
						}

						// Force check to see if we should stop service
						ReportStatus(worker, "");
					}
				}
			}
			catch (Exception error)
			{
				ReportStatus(worker, string.Format("Error {0}: {1}", error.GetType().Name, error.Message));
			}

			ReportStatus(worker, "Service Stopped");
		}

		public bool Running { get; private set; }
	}

	public class DistinctWordComparer : IEqualityComparer<WordSequence>
	{
		public bool Equals(WordSequence x, WordSequence y)
		{
			return x.Word.ToLower() == x.Word.ToLower();
		}

		public int GetHashCode(WordSequence obj)
		{
			return obj.Word.ToLower().GetHashCode();
		}
	}
}