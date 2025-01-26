using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

using dotmob;

namespace WordCross
{
	public class RankedWord
	{
		public string	word;
		public int		rank;

		public RankedWord(string word, int rank)
		{
			this.word = word;
			this.rank = rank;
		}
	}

	public enum BoardType
	{
		Grid,
		List
	}

	public enum LengthPickType
	{
		Random,
		LongestFirst,
		ShortestFirst,
		DistributedLongestFirst,
		DistributedShortestFirst
	}

	public enum RankPickType
	{
		Random,
		MostCommonFirst,
		LeastCommonFirst
	}

	public static class LevelBuilderUtilities
	{
		#region Public Methods

		/// <summary>
		/// Gets all the words whos letters are subsets of the given letters.
		/// </summary>
		public static List<RankedWord> GetWordsThatFit(string letters, int length, Dictionary<string, List<RankedWord>> wordDictionary, Dictionary<int, List<RankedWord>> wordsByLength)
		{
			string			sortedLetters				= SortAlphabetically(letters);
			List<string>	sortedLetterCombinations	= GetLetterCombinations(sortedLetters);
			List<RankedWord>		words						= new List<RankedWord>();

			// Get all the words from the word dictionary that have the same letters as any of the letter combinations
			for (int i = 0; i < sortedLetterCombinations.Count; i++)
			{
				string sortedLetterCombination = sortedLetterCombinations[i];

				if (wordDictionary.ContainsKey(sortedLetterCombination))
				{
					words.AddRange(wordDictionary[sortedLetterCombination]);
				}
			}

			int extraLetters = length - letters.Length;

			if (extraLetters > 0)
			{
				// Get a dictionary where the key is each character in letters and the value is the number of those characters that appear in letters
				Dictionary<char, int> letterDictionary = new Dictionary<char, int>();

				for (int i = 0; i < sortedLetters.Length; i++)
				{
					char letter = sortedLetters[i];

					if (!letterDictionary.ContainsKey(letter))
					{
						letterDictionary.Add(letter, 0);
					}

					letterDictionary[letter]++;
				}

				// Get all words whos length is greater than letters.Length and less than or equal to the given length
				for (int i = letters.Length + 1; i <= length; i++)
				{
					int wordLength = i;

					if (wordsByLength.ContainsKey(wordLength))
					{
						List<RankedWord>	wordsToTry			= wordsByLength[wordLength];
						int			minSharedLetters	= (wordLength - extraLetters);

						if (minSharedLetters <= 0)
						{
							words.AddRange(wordsToTry);
						}
						else
						{
							words.AddRange(GetWordsThatFit(wordsToTry, letterDictionary, minSharedLetters));
						}
					}
				}
			}

			return words;
		}

		/// <summary>
		/// Returns a string of all the letters needed to make all the words
		/// </summary>
		public static string GetLetters(List<string> words)
		{
			string letters = "";

			for (int i = 0; i < words.Count; i++)
			{
				string word = words[i];

				// Get a dictionary of of letters that are already in choosenSortedLetters
				Dictionary<char, int> letterDictionary = new Dictionary<char, int>();

				for (int j = 0; j < letters.Length; j++)
				{
					char letter = letters[j];

					if (!letterDictionary.ContainsKey(letter))
					{
						letterDictionary.Add(letter, 0);
					}

					letterDictionary[letter]++;
				}

				for (int j = 0; j < word.Length; j++)
				{
					char letter	= word[j];
					bool inDic	= letterDictionary.ContainsKey(letter);

					// If it's in the letterDictionary already then we don't need to add it
					if (inDic)
					{
						letterDictionary[letter]--;

						if (letterDictionary[letter] == 0)
						{
							letterDictionary.Remove(letter);
						}
					}
					else
					{
						letters += letter;
					}
				}

				letters = SortAlphabetically(letters);
			}

			return letters;
		}

		/// <summary>
		/// Returns a string where all from word are sorted based on their char integer value
		/// </summary>
		public static string SortAlphabetically(string word)
		{
			List<char> wordCharacters = new List<char>(word.ToCharArray());

			wordCharacters.Sort();

			string sortedWord = "";

			for (int i = 0; i < wordCharacters.Count; i++)
			{
				sortedWord += wordCharacters[i];
			}

			return sortedWord;
		}

		/// <summary>
		/// Gets the full path to the output folder
		/// </summary>
		public static string GetOutputFolderPath(Object outputFolder)
		{
			string folderPath = GetFolderPath(outputFolder);

			// If the folder path is null then set the path to the Resources folder
			if (string.IsNullOrEmpty(folderPath))
			{
				folderPath = Application.dataPath + "/Resources";
			}

			return folderPath;
		}

		/// <summary>
		/// Gets the folder path.
		/// </summary>
		public static string GetFolderPath(Object folderObject)
		{
			string folderPath = "";

			if (folderObject != null)
			{
				// Get the full system path to the folder
				folderPath = Application.dataPath.Remove(Application.dataPath.Length - "Assets".Length) + UnityEditor.AssetDatabase.GetAssetPath(folderObject);

				// If it's not a folder then set the path to null so the default path is choosen
				if (!System.IO.Directory.Exists(folderPath))
				{
					folderPath = "";
				}
			}

			return folderPath;
		}

		/// <summary>
		/// Exports the level file.
		/// </summary>
		public static bool ExportLevelFile(string					letters,
		                                   BoardType				boardType,
		                                   GridBoardWorker.Board	board,
		                                   List<string>				choosenWords,
		                                   List<bool>				starredChoosenWords,
		                                   bool						batchMode,
		                                   int						currentBatchFileNumber,
		                                   string					folderPath,
		                                   string					filename,
		                                   bool 					overwrite)
		{
			// If the folder does not exist then create it
			if (!System.IO.Directory.Exists(folderPath))
			{
				System.IO.Directory.CreateDirectory(folderPath);
			}

			// Get the levels json
			string levelId		= "";
			string levelJson	= CreateLevelFileJson(letters, boardType, board, choosenWords, starredChoosenWords, out levelId);
			string fullFilePath	= "";

			if (batchMode)
			{
				fullFilePath = string.Format("{0}/{1}{2}.json", folderPath, filename, currentBatchFileNumber);

				if (System.IO.File.Exists(fullFilePath) && !overwrite)
				{
					fullFilePath = GetUniqueBatchFileName(folderPath, filename, currentBatchFileNumber);
				}
			}
			else
			{
				fullFilePath = string.Format("{0}/{1}.json", folderPath, string.IsNullOrEmpty(filename) ? levelId : filename);

				if (System.IO.File.Exists(fullFilePath) && !overwrite)
				{
					return false;
				}
			}

			System.IO.File.WriteAllText(fullFilePath, levelJson);

			return true;
		}

		/// <summary>
		/// Loads the level files contents
		/// </summary>
		public static bool LoadLevelFile(string contents, out string letters, out List<string> wordsInLevel, out List<bool> wordIsStarred, out BoardType boardType, out GridBoardWorker.Board board)
		{
			letters			= "";
			boardType		= BoardType.List;
			board			= null;
			wordsInLevel	= null;
			wordIsStarred	= null;

			try
			{
				JSONNode levelJson = JSON.Parse(contents);

				if (levelJson == null)
				{
					Debug.LogError("Could not load level file, invalid json.");

					return false;
				}

				boardType	= (BoardType)levelJson["type"].AsInt;
				letters		= levelJson["letters"].Value;

				JSONArray wordsJson	= levelJson["words"].AsArray;

				wordsInLevel	= new List<string>();
				wordIsStarred	= new List<bool>();

				if (boardType == BoardType.Grid)
				{
					board				= new GridBoardWorker.Board();
					board.numRows		= levelJson["rowCount"].AsInt;
					board.numCols		= levelJson["colCount"].AsInt;
					board.boardWords	= new List<GridBoardWorker.BoardWord>();
				}

				foreach (JSONNode wordJson in wordsJson)
				{
					string word = wordJson["word"].Value;

					wordsInLevel.Add(word);
					wordIsStarred.Add(wordJson["awardCoins"].AsBool);

					if (boardType == BoardType.Grid)
					{
						GridBoardWorker.BoardWord boardWord = new GridBoardWorker.BoardWord();

						boardWord.word			= word;
						boardWord.isVertical	= wordJson["vertical"].AsBool;
						boardWord.rowIndex		= wordJson["rowIndex"].AsInt;
						boardWord.colIndex		= wordJson["colIndex"].AsInt;

						board.boardWords.Add(boardWord);
					}
				}

				if (string.IsNullOrEmpty(letters) || wordsInLevel.Count == 0)
				{
					Debug.LogError("Could not load level file, incorrect number of letters and/or words.");

					return false;
				}

				if (board != null)
				{
					GridBoardWorker.SetBoardLettersArray(board);
				}

				return true;
			}
			catch (System.Exception ex)
			{
				Debug.LogError("Could not load level file, exception: " + ex.Message + "\n\n*** Stack Trace ***\n\n" + ex.StackTrace + "\n\n**********************");
			}

			return false;
		}

		#endregion

		#region Private Methods

		private static List<RankedWord> GetWordsThatFit(List<RankedWord> wordsToTry, Dictionary<char, int> letterDictionary, int minSharedLetters)
		{
			List<RankedWord> words = new List<RankedWord>();

			for (int i = 0; i < wordsToTry.Count; i++)
			{
				Dictionary<char, int> letterDictionaryCopy = new Dictionary<char, int>(letterDictionary);

				string	word			= wordsToTry[i].word;
				int		sharedLetters	= 0;

				for (int j = 0; j < word.Length; j++)
				{
					char letter = word[j];

					// Check if the letters dictionary contain this letter
					if (letterDictionaryCopy.ContainsKey(letter))
					{
						sharedLetters++;

						// Check if we now meet the minimum number of shared letters
						if (sharedLetters >= minSharedLetters)
						{
							words.Add(wordsToTry[i]);

							break;
						}

						letterDictionaryCopy[letter]--;

						if (letterDictionaryCopy[letter] == 0)
						{
							letterDictionaryCopy.Remove(letter);
						}
					}
				}
			}

			return words;
		}

		private static List<string> GetLetterCombinations(string letters)
		{
			List<string> letterCombinations = new List<string>();

			// Get a list of all letter combinations (For example: abcd will become ab, ac, ad, bc, bd, cd, abc, abd, acd, bcd)
			for (int i = 0; i < letters.Length; i++)
			{
				List<string> combos = GetLetterCombinations(letters[i].ToString(), letters.Remove(0, i + 1));

				for (int j = 0; j < combos.Count; j++)
				{
					if (!letterCombinations.Contains(combos[j]))
					{
						letterCombinations.Add(combos[j]);
					}
				}
			}

			return letterCombinations;
		}

		private static List<string> GetLetterCombinations(string soFar, string letters)
		{
			List<string> letterCombinations = new List<string>();

			for (int i = 0; i < letters.Length; i++)
			{
				string newCombo = soFar + letters[i];

				letterCombinations.Add(newCombo);
				letterCombinations.AddRange(GetLetterCombinations(newCombo, letters.Remove(0, i + 1)));
			}

			return letterCombinations;
		}

		private static string CreateLevelFileJson(string				letters,
		                                          BoardType				boardType,
		                                          GridBoardWorker.Board	board,
		                                          List<string>			choosenWords,
		                                          List<bool>			starredChoosenWords)
		{
			string levelId;

			return CreateLevelFileJson(letters, boardType, board, choosenWords, starredChoosenWords, out levelId);
		}

		private static string CreateLevelFileJson(string					letters,
		                                          BoardType					boardType,
		                                          GridBoardWorker.Board		board,
		                                          List<string>				choosenWords,
		                                          List<bool>				starredChoosenWords,
		                                          out string				levelId)
		{
			Dictionary<string, object>	json		= new Dictionary<string, object>();
			List<object>				wordsJson	= new List<object>();

			if (boardType == BoardType.Grid && board != null)
			{
				for (int i = 0; i < board.boardWords.Count; i++)
				{
					GridBoardWorker.BoardWord boardWord = board.boardWords[i];

					Dictionary<string, object> wordJson = new Dictionary<string, object>();

					wordJson["word"]		= boardWord.word;
					wordJson["vertical"]	= boardWord.isVertical;
					wordJson["rowIndex"]	= boardWord.rowIndex;
					wordJson["colIndex"]	= boardWord.colIndex;
					wordJson["coinIndices"]	= GetCoinIndicies(board, boardWord, choosenWords, starredChoosenWords);
					wordJson["awardCoins"]	= IsWordStarred(boardWord.word, choosenWords, starredChoosenWords);

					wordsJson.Add(wordJson);
				}

				json["rowCount"] = board.numRows;
				json["colCount"] = board.numCols;
			}
			else
			{
				for (int i = 0; i < choosenWords.Count; i++)
				{
					Dictionary<string, object> wordJson = new Dictionary<string, object>();

					wordJson["word"]		= choosenWords[i];
					wordJson["awardCoins"]	= starredChoosenWords[i];

					wordsJson.Add(wordJson);
				}
			}

			json["type"]		= (int)boardType;
			json["letters"]		= letters;
			json["words"]		= wordsJson;

			levelId = GetLevelId(Utilities.ConvertToJsonString(json));

			// The level id is generated by taking the level json without the id field and getting it's md5 hash
			json["id"] = levelId;

			// Now get the json with the id
			return Utilities.ConvertToJsonString(json);
		}

		private static string GetLevelId(string json)
		{

			// encrypt bytes
			MD5CryptoServiceProvider	md5			= new MD5CryptoServiceProvider();
			byte[]						hashBytes	= md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));

			// Convert the encrypted bytes back to a string (base 16)
			string hashString = "";

			for (int i = 0; i < hashBytes.Length; i++)
			{
				hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
			}

			return hashString.PadLeft(32, '0');
		}

		/// <summary>
		/// Returns all letter indices in the word that are part of a cell with a coin
		/// </summary>
		private static List<int> GetCoinIndicies(GridBoardWorker.Board board, GridBoardWorker.BoardWord boardWord, List<string> choosenWords, List<bool> starredChoosenWords)
		{
			bool		isWordStarred	= IsWordStarred(boardWord.word, choosenWords, starredChoosenWords);
			List<int>	coinIndices		= new List<int>();

			for (int i = 0; i < boardWord.word.Length; i++)
			{
				if (isWordStarred)
				{
					// Add all the indicies if this word is starred
					coinIndices.Add(i);
				}
				else
				{
					int rowIndex = boardWord.rowIndex + (boardWord.isVertical ? i : 0);
					int colIndex = boardWord.colIndex + (boardWord.isVertical ? 0 : i);

					GridBoardWorker.BoardWord crossingBoardWord = GetCrossingWord(board, rowIndex, colIndex, !boardWord.isVertical);

					// Add the index if the crossing word is starred
					if (crossingBoardWord != null && IsWordStarred(crossingBoardWord.word, choosenWords, starredChoosenWords))
					{
						coinIndices.Add(i);
					}
				}
			}

			return coinIndices;
		}

		/// <summary>
		/// Gets the BoardWord that crosses the given cell in the given direction, null if there is no word.
		/// </summary>
		private static GridBoardWorker.BoardWord GetCrossingWord(GridBoardWorker.Board board, int rowIndex, int colIndex, bool vertical)
		{
			for (int i = 0; i < board.boardWords.Count; i++)
			{
				GridBoardWorker.BoardWord boardWord = board.boardWords[i];

				if (boardWord.isVertical != vertical)
				{
					continue;
				}

				int endRow = boardWord.rowIndex + (vertical ? boardWord.word.Length : 0) - 1;
				int endCol = boardWord.colIndex + (vertical ? 0 : boardWord.word.Length) - 1;

				if (vertical && colIndex == boardWord.colIndex && rowIndex >= boardWord.rowIndex && rowIndex <= endRow)
				{
					return boardWord;
				}

				if (!vertical && rowIndex == boardWord.rowIndex && colIndex >= boardWord.colIndex && colIndex <= endCol)
				{
					return boardWord;
				}
			}

			return null;
		}

		private static bool IsWordStarred(string word, List<string> choosenWords, List<bool> starredChoosenWords)
		{
			for (int i = 0; i < choosenWords.Count; i++)
			{
				if (word == choosenWords[i])
				{
					return starredChoosenWords[i];
				}
			}

			return false;
		}

		private static string GetUniqueBatchFileName(string folderPath, string namePrefix, int startNumber)
		{
			string	fullFilePath	= "";
			int		number			= startNumber;

			while (System.IO.File.Exists(fullFilePath = string.Format("{0}/{1}{2}.json", folderPath, namePrefix, number)))
			{
				number++;
			}

			return fullFilePath;
		}

		#endregion
	}
}
