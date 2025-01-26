using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WordCross
{
	public class LoadExtraWordsWorker : Worker
	{
		public string			wordFileContents;
		public HashSet<string>	allWords;

		protected override void Begin()
		{
			allWords = new HashSet<string>();
		}

		protected override void DoWork()
		{
			string[] words = wordFileContents.Split('\n');

			for (int i = 0; i < words.Length; i++)
			{
				string word = words[i].Replace("\r", "").Trim().ToLower();

				if (!allWords.Contains(word))
				{
					allWords.Add(word);
				}
			}

			Stop();
		}
	}
}
