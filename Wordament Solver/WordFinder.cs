using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Wordament
{
	public class WordSequence
	{
		public WordSequence(Point[] tiles, string word)
		{
			Tiles = tiles;
			Word = word;
		}

		public Point[] Tiles { get; private set; }

		public string Word { get; private set; }
	}

	public class WordFinder
	{
		private IWordList dict;
		private bool[,] used;
		private string[,] board;
		private int w;
		private int h;
		private int depth;
		private string[] letters;
		private Point[] places;

		public int CombinationCount { get; private set; }

		public IEnumerable<string> FindWords(IWordList dictionary, string[,] board)
		{
			foreach (var seq in FindWordSequences(dictionary, board))
			{
				yield return seq.Word;
			}
		}

		public IEnumerable<WordSequence> FindWordSequences(IWordList dictionary, string[,] board)
		{
			dict = dictionary;

			w = board.GetUpperBound(0) + 1;
			h = board.GetUpperBound(1) + 1;

			letters = new string[w * h];
			places = new Point[w * h];

			this.board = board;

			used = new bool[w, h];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					{
						used[x, y] = false;
						letters[x + (w * y)] = "";
					}

			depth = 0;

			CombinationCount = 0;

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					string currentLetter = board[x, y];
					if (currentLetter.Contains("/"))
					{
						foreach (var letter in currentLetter.Split('/'))
							foreach (var seq in FindWordSequences(x, y, letter))
								yield return seq;
					}
					else
					{
						foreach (var seq in FindWordSequences(x, y, currentLetter))
							yield return seq;
					}
				}
			}
		}

		private IEnumerable<WordSequence> FindWordSequences(int currentX, int currentY, string currentLetter)
		{
			letters[depth] = currentLetter;
			used[currentX, currentY] = true;
			places[depth].X = currentX;
			places[depth].Y = currentY;
			CombinationCount++;
			
			depth++;

			bool noWordsWithPrefix = false;

			if (depth >= 3)
			{
				string currentWord = string.Join("", letters, 0, depth);
				if (dict.Contains(currentWord))
				{
					var tiles = new Point[depth];
					for (var tileIndex = 0; tileIndex < depth; tileIndex++)
						tiles[tileIndex] = places[tileIndex];

					yield return new WordSequence(tiles, currentWord);
				}

				noWordsWithPrefix = !dict.ContainsStartingWith(currentWord);
			}

			if (!noWordsWithPrefix)
			{
				for (int plusY = -1; plusY <= 1; plusY++)
				{
					for (int plusX = -1; plusX <= 1; plusX++)
					{
						if (plusX == 0 && plusY == 0)
							continue;

						int newX = currentX + plusX;
						int newY = currentY + plusY;

						if ((newX < 0) || (newX >= w) || (newY < 0) || (newY >= h))
							continue;

						if (used[newX, newY])
							continue;

						string nextLetter = board[newX, newY];
						if (nextLetter.Contains("/"))
						{
							foreach (var letter in currentLetter.Split('/'))
								foreach (var seq in FindWordSequences(newX, newY, letter))
									yield return seq;
						}
						else
						{
							foreach (var seq in FindWordSequences(newX, newY, nextLetter))
								yield return seq;
						}
					}
				}
			}

			depth--;
			letters[depth] = "";
			used[currentX, currentY] = false;
		}
	}
}