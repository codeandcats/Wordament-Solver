using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Wordament;

namespace Wordament
{
	[TestClass]
	public class WordListTests
	{
		[TestInitialize]
		public void Setup()
		{
		}

		[TestCleanup]
		public void Teardown()
		{
		}

		[TestMethod]
		public void TestBinarySearch()
		{
			List<string> animalsUnordered = new List<string>(
				new[] { "Cat", "Dog", "Elephant", "Eagle", "Eel", "Zebra", "Yak", "Monkey", "Meerkat" });

			WordList words;

			while (animalsUnordered.Count > 0)
			{	
				words = new WordList(animalsUnordered);
				
				Assert.AreEqual(-1, words.BinarySearch("Unicorn", false, false), "Wrong index for: Unicorn");

				string[] animalsOrdered = animalsUnordered.OrderBy(a => a).ToArray();

				Assert.AreEqual(-1, words.BinarySearch("Dragon", false, false), "Wrong index for: Dragon");

				for (int expectedIndex = 0; expectedIndex < animalsOrdered.Length; expectedIndex++)
				{
					string word = animalsOrdered[expectedIndex];
					Assert.AreEqual(expectedIndex, words.BinarySearch(word, false, false), "Wrong index for: " + word);
				}

				animalsUnordered.RemoveAt(0);
			}

			words = new WordList(new[] { "Heaven", "Hell", "Hello", "Zebra", "ZOO" });

			Assert.AreEqual(0, words.BinarySearch("H", false, true), "Wrong index for: H");
			Assert.AreEqual(1, words.BinarySearch("HELL", false, true), "Wrong index for: H");
			Assert.AreEqual(3, words.BinarySearch("M", true, false), "Wrong index for: M");
			Assert.AreEqual(-1, words.BinarySearch("M", false, false), "Wrong index for: M");
		}
	}
}