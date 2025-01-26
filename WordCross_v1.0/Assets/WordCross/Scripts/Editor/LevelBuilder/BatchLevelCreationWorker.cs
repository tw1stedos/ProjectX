using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WordCross
{
	public class BatchLevelCreationWorker : Worker
	{
		#region Inspector Variables

		#endregion

		#region Member Variables

		/// <summary>
		/// Since this worker will be run in a seperate thread we cannt use UnityEngine.Random
		/// </summary>
		private System.Random random;

		private int currentBatchIndex;

		#endregion

		#region Properties

		// In Properties
		public Dictionary<string, List<RankedWord>>	WordDictionary		{ get; set; }
		public Dictionary<int, List<RankedWord>>	WordsByLength		{ get; set; }

		public bool				CreateBoard			{ get; set; }
		public float			PreferredBoardRatio	{ get; set; }
		public int				MaxBoards			{ get; set; }

		public int				LetterCount			{ get; set; }
		public int				MinWords			{ get; set; }
		public int				MaxWords			{ get; set; }
		public int				NumberOfLevels		{ get; set; }
		public int				MaxWordLength		{ get; set; }
		public int				MinWordLength		{ get; set; }
		public LengthPickType	WordLengthPickType	{ get; set; }
		public RankPickType		WordRankPickType	{ get; set; }

		public bool				ChooseBonusWord		{ get; set; }
		public int				EveryNumberOfLevels	{ get; set; }
		public bool				IncludeFirstLevel	{ get; set; }
		public int				MaxBonusWordLength	{ get; set; }
		public int				MinBonusWordLength	{ get; set; }

		public bool				ReuseWords			{ get; set; }
		public string			LoadFromFolderPath	{ get; set; }
		public int				MaxReuseWordLength	{ get; set; }
		public int				MinReuseWordLength	{ get; set; }

		public string			OutputFolderPath	{ get; set; }
		public string			FilenamePrefix		{ get; set; }
		public bool				OverwriteExisting	{ get; set; }

		// Out Properties
		public string Message { get; set; }

		private bool ShouldChooseBonusWord { get { return ChooseBonusWord && ((currentBatchIndex + 1) % EveryNumberOfLevels) == 0 && (IncludeFirstLevel || currentBatchIndex != 0); } }

		#endregion

		#region Public Methods

		protected override void Begin()
		{
			random = new System.Random();
		}

		protected override void DoWork()
		{
			try
			{
				List<string> excludedWords = new List<string>();

				// Check if we need to load all the words in all the level files already created
				if (!string.IsNullOrEmpty(LoadFromFolderPath))
				{
					excludedWords = LoadWords(LoadFromFolderPath);

					Debug.Log("Loaded " + excludedWords.Count + " words from the folder that will be not be used in levels");
				}

				// Generate the required number of levels
				for (currentBatchIndex = 0; currentBatchIndex < NumberOfLevels; currentBatchIndex++)
				{
					// Set the progress
					Progress = (float)currentBatchIndex / (float)NumberOfLevels;

					List<string> choosenWords = new List<string>();

					// Get the level data
					object[] data = CreateLevel(choosenWords, excludedWords);

					if (Stopping)
					{
						return;
					}

					// Sanity check to make sure if the data is not null (It shouldn't be at this point)
					if (data == null)
					{
						StopGeneratingLevels("Null level data");
					}
					else
					{
						// Export the level
						int						levelNumber		= currentBatchIndex + 1;
						string					letters			= data[0] as string;
						GridBoardWorker.Board	board			= data[2] as GridBoardWorker.Board;
						BoardType				boardType		= board != null ? BoardType.Grid : BoardType.List;

						List<bool> starredChoosenWords = new List<bool>();

						for (int j = 0; j < choosenWords.Count; j++)
						{
							starredChoosenWords.Add(false);
						}

						if (ShouldChooseBonusWord)
						{
							starredChoosenWords[0] = true;
						}

						LevelBuilderUtilities.ExportLevelFile(letters, boardType, board, choosenWords, starredChoosenWords, true, levelNumber, OutputFolderPath, FilenamePrefix, OverwriteExisting);

						// Add any of the choosen words that cannot be reused
						for (int j = 0; j < choosenWords.Count; j++)
						{
							string word = choosenWords[j];

							if (ExcludeWord(word))
							{
								excludedWords.Add(word);
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				StopGeneratingLevels(ex.Message + "\n" + ex.StackTrace);
			}

			Stop();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Loads all the words for all the level files in the given folder
		/// </summary>
		private List<string> LoadWords(string folderPath)
		{
			List<string> levelFiles	= Utilities.GetFilesRecursively(folderPath, "*.json");
			List<string> words		= new List<string>();

			for (int i = 0; i < levelFiles.Count; i++)
			{
				string contents = System.IO.File.ReadAllText(levelFiles[i]);

				string					letters;
				List<string>			wordsInLevel;
				List<bool>				wordIsStarred;
				BoardType				boardType;
				GridBoardWorker.Board	board;

				if (LevelBuilderUtilities.LoadLevelFile(contents, out letters, out wordsInLevel, out wordIsStarred, out boardType, out board))
				{
					// Add any of the words from the level that are excluded
					for (int j = 0; j < wordsInLevel.Count; j++)
					{
						string word = wordsInLevel[j];

						if (ExcludeWord(word) && !words.Contains(word))
						{
							words.Add(word);
						}
					}
				}
			}

			return words;
		}

		private object[] CreateLevel(List<string> choosenWords, List<string> excludedWords)
		{
			if (Stopping)
			{
				return null;
			}

			// Get the letters in the level
			string letters = LevelBuilderUtilities.GetLetters(choosenWords);

			// Check if MaxWords has been set and we have reached the max words in a level
			if (MaxWords != 0 && choosenWords.Count >= MaxWords)
			{
				return FinishedCreatingLevel(letters, choosenWords);
			}

			bool	chooseBonusWord	= (ShouldChooseBonusWord && choosenWords.Count == 0);
			int		minWordLength	= chooseBonusWord ? MinBonusWordLength : MinWordLength;
			int		maxWordLength	= chooseBonusWord ? MaxBonusWordLength : MaxWordLength;

			// Get an updated list of all the possible words we cna choose from
			List<List<string>> allPossibleWords = GetPossibleWords(letters, choosenWords, excludedWords, minWordLength, maxWordLength);

			// We will keep trying words until there are no more words to try
			while (allPossibleWords.Count > 0)
			{
				if (Stopping)
				{
					return null;
				}

				string word = "";

				// Check if we need to choose a bonus word first
				if (chooseBonusWord)
				{
					word = PickBonusWord(allPossibleWords);
				}
				else
				{
					word = PickWord(allPossibleWords, choosenWords.Count);
				}

				choosenWords.Add(word);

				// Try and create the rest of the level with this word choosen
				object[] levelData = CreateLevel(choosenWords, excludedWords);

				// If levelData is not null then we successfully created the level so return
				if (levelData != null)
				{
					return levelData;
				}

				// We did not successfully create the level with the choosen word so remove it and try again with another word
				choosenWords.RemoveAt(choosenWords.Count - 1);
			}

			// Either there were no possible words to begin with or none of the possible words lead to a successful board
			return NoMoreWords(letters, choosenWords);
		}

		/// <summary>
		/// Picks a bonus word to use in the level. Removes the word from allPossibleWords.
		/// </summary>
		private string PickBonusWord(List<List<string>> allPossibleWords)
		{
			// Pick a random word to use as the bonus from the top half of the list
			int				randLength	= random.Next(0, allPossibleWords.Count);
			List<string>	words		= allPossibleWords[randLength];
			int				halfIndex	= Mathf.FloorToInt((float)words.Count / 2f);
			int				randWord	= random.Next(halfIndex, words.Count);

			string word = words[randWord];

			words.RemoveAt(randWord);

			if (words.Count == 0)
			{
				allPossibleWords.RemoveAt(randLength);
			}

			return word;
		}

		/// <summary>
		/// Picks a word to use in the level. Removes the word from allPossibleWords.
		/// </summary>
		private string PickWord(List<List<string>> allPossibleWords, int numChoosenSoFar)
		{
			int index = 0;

			// Choose a list of words to pick from based on the WordLengthPickType
			if (WordLengthPickType == LengthPickType.DistributedLongestFirst || WordLengthPickType == LengthPickType.DistributedShortestFirst)
			{
				index = numChoosenSoFar % allPossibleWords.Count;
			}
			else
			{
				index = random.Next(0, allPossibleWords.Count);
			}

			List<string> wordsToChooseFrom = allPossibleWords[index];

			int wordIndex = 0;

			// Choose a word from the list based on the WordRankPickType
			switch (WordRankPickType)
			{
				case RankPickType.MostCommonFirst:
					wordIndex = 0;
					break;
				case RankPickType.LeastCommonFirst:
					wordIndex = wordsToChooseFrom.Count - 1;
					break;
				case RankPickType.Random:
				default:
					wordIndex = random.Next(0, wordsToChooseFrom.Count);
					break;
			}

			string word = wordsToChooseFrom[wordIndex];

			wordsToChooseFrom.RemoveAt(wordIndex);

			if (wordsToChooseFrom.Count == 0)
			{
				allPossibleWords.RemoveAt(index);
			}

			return word;
		}

		/// <summary>
		/// Called when there are no more possible words to choose from. Checks if the level and valid.
		/// </summary>
		private object[] NoMoreWords(string letters, List<string> choosenWords)
		{
			// If we have not choosen any words yet then there are no more words left to choose from for any level, so we must stop generating levels
			if (choosenWords.Count == 0)
			{
				StopGeneratingLevels("No more words to use");

				return null;
			}

			// If we did not get enough words for the level then return null to indicate these choosen words are not valid
			if (choosenWords.Count < MinWords)
			{
				return null;
			}

			// This is a valid level and there are no more possible words to choose from so we finish this level
			return FinishedCreatingLevel(letters, choosenWords);
		}

		/// <summary>
		/// Creates the level data and generates the Board (If a board needs to be generated). Can still return null if no valid Board was created.
		/// </summary>
		private object[] FinishedCreatingLevel(string letters, List<string> choosenWords)
		{
			// Found a valid list of words for the level
			object[] levelData = new object[3];

			levelData[0] = letters;
			levelData[1] = choosenWords;

			if (CreateBoard)
			{
				GridBoardWorker gridBoardWorker = new GridBoardWorker();

				gridBoardWorker.Words			= new List<string>(choosenWords);
				gridBoardWorker.MaxBoards		= MaxBoards;
				gridBoardWorker.PreferredRatio	= PreferredBoardRatio;

				if (ShouldChooseBonusWord)
				{
					gridBoardWorker.BonusWord = gridBoardWorker.Words[0];
					gridBoardWorker.Words.RemoveAt(0);
				}

				gridBoardWorker.Run();

				if (gridBoardWorker.BestBoard == null)
				{
					// Could not find a board that is valid for the given letters so the list of choosen words is not valid
					return null;
				}

				levelData[2] = gridBoardWorker.BestBoard;
			}

			// Found a valid level
			return levelData;
		}

		/// <summary>
		/// Gets a list of all possible words that can go with the current choosen words
		/// </summary>
		private List<List<string>> GetPossibleWords(string letters, List<string> choosenWords, List<string> excludedWords, int minLength, int maxLength)
		{
			List<RankedWord> possibleWords = null;

			// If choosenSortedLetters is empty then just get the list of words that have length equal to letterCount
			if (string.IsNullOrEmpty(letters))
			{
				possibleWords = new List<RankedWord>();

				for (int len = LetterCount; len >= 2; len--)
				{
					if (WordsByLength.ContainsKey(len))
					{
						possibleWords.AddRange(WordsByLength[len]);
					}
				}
			}
			// Else get all possible words for the level given the choosen words and the letterCount
			else
			{
				possibleWords = LevelBuilderUtilities.GetWordsThatFit(letters, LetterCount, WordDictionary, WordsByLength);
			}

			// Remove choosen and excluded words and words that are to long/short
			for (int i = possibleWords.Count - 1; i >= 0; i--)
			{
				string word = possibleWords[i].word;

				if (choosenWords.Contains(word) ||
				    excludedWords.Contains(word) ||
				    (minLength != 0 && word.Length < minLength) ||
				    (maxLength != 0 && word.Length > maxLength))
				{
					possibleWords.RemoveAt(i);
				}
			}

			// Sort the possible words by length then by rank
			possibleWords.Sort((RankedWord rankedWord1, RankedWord rankedWord2) =>
			{
				int lengthDiff = rankedWord1.word.Length - rankedWord2.word.Length;

				if (WordLengthPickType == LengthPickType.LongestFirst || WordLengthPickType == LengthPickType.DistributedLongestFirst)
				{
					lengthDiff = rankedWord2.word.Length - rankedWord1.word.Length;
				}

				if (lengthDiff != 0)
				{
					return lengthDiff;
				}

				return rankedWord1.rank - rankedWord2.rank;
			});

			List<List<string>> possibleWordsByLength = new List<List<string>>();

			// Split the list of words up by their length
			for (int i = 0; i < possibleWords.Count; i++)
			{
				if (i == 0 || possibleWords[i].word.Length != possibleWords[i - 1].word.Length)
				{
					possibleWordsByLength.Add(new List<string>());
				}

				possibleWordsByLength[possibleWordsByLength.Count - 1].Add(possibleWords[i].word);
			}

			return possibleWordsByLength;
		}

		private bool ExcludeWord(string word)
		{
			return!ReuseWords ||
			    (MinReuseWordLength != 0 && word.Length < MinReuseWordLength) || 
				(MaxReuseWordLength != 0 && word.Length > MaxReuseWordLength);
		}

		/// <summary>
		/// Stops generating levels
		/// </summary>
		private void StopGeneratingLevels(string message)
		{
			Message = message;

			Stop();
		}

		#endregion
	}
}
