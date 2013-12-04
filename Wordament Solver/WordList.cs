using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Wordament
{
	public interface IWordList
	{
		void LoadFromFile(string fileName, bool autoAddPlurals);
		bool Contains(string word);
		int Count { get; }
		bool ContainsStartingWith(string text);
	}

	public class WordList : IWordList
	{
		private HashSet<string> hash = new HashSet<string>();
		private List<string> list = new List<string>();

		public WordList()
		{
		}

		public WordList(IEnumerable<string> words)
		{
			foreach (var word in words.OrderBy(w => w))
				AddWord(word);
		}

		public void LoadFromFile(string fileName, bool autoAddPlurals)
		{
			hash = new HashSet<string>();
			list = new List<string>();

			using (var reader = new StreamReader(fileName))
			{
				while (!reader.EndOfStream)
				{
					string word = reader.ReadLine().Trim();

					if (AddWord(word) && autoAddPlurals)
					{
						if (word.Length > 1)
							if (word[word.Length - 1] != 's')
							{
								if (word.Substring(word.Length - 2, 2) == "ty")
									AddWord(word.Substring(0, word.Length - 2) + "ties");
								else if (word[word.Length - 1] == 'x')
									AddWord(word + "es");
								else
									AddWord(word + 's');
							}
					}
				}
			}
		}

		private bool AddWord(string word)
		{
			word = word.Replace("-", "").Replace("'", "").Trim().ToLower();
			if (word == "")
				return false;
			if (hash.Add(word))
			{
				list.Add(word);
				return true;
			}
			return false;
		}

		public bool Contains(string word)
		{
			return hash.Contains(word.ToLower());
		}

		public int Count
		{
			get { return hash.Count; }
		}

		public bool ContainsStartingWith(string text)
		{
			return BinarySearch(text, false, true) > -1;
		}

		internal int BinarySearch(string word, bool closestMatch, bool startingWith)
		{
			if (list.Count == 0)
				return -1;

			word = word.ToLower();

			int minIndex = 0;
			int maxIndex = list.Count - 1;
			int midIndex = -1;

			while (maxIndex >= minIndex)
			{
				midIndex = minIndex + (int)Math.Truncate((maxIndex - minIndex) / 2f);

				string midWord = list[midIndex];

				if (startingWith && (midWord.Length > word.Length))
					midWord = midWord.Substring(0, word.Length);

				int compare = word.CompareTo(midWord);

				if (compare < 0)
					maxIndex = midIndex - 1;
				else if (compare > 0)
					minIndex = midIndex + 1;
				else
				{
					if (!startingWith)
						return midIndex;
				
					while ((midIndex > minIndex) && (list[midIndex - 1].StartsWith(word)))
					{
						midIndex--;
					}
					return midIndex;
				}
			};

			if (!closestMatch)
				return -1;

			return minIndex;
		}
	}
}
