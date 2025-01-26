using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using dotmob;

namespace WordCross
{
	public class LevelData
	{
		#region Enums

		public enum Type
		{
			Grid,
			List
		}

		#endregion

		#region Member Variables

		private string	levelFileContents;
		private bool	isLoaded;

		private string			id;
		private Type			levelType;
		private string			letters;
		private List<WordData>	words;
		private int				boardRows;
		private int				boardCols;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the id of this level
		/// </summary>
		public string Id
		{
			get
			{
				if (!isLoaded)
				{
					ParseLevelFile();
				}

				return id;
			}
		}

		/// <summary>
		/// Gets this levels type, Grid or List
		/// </summary>
		public Type LevelType
		{
			get
			{
				if (!isLoaded)
				{
					ParseLevelFile();
				}

				return levelType;
			}
		}

		/// <summary>
		/// Gets the letters in this level that make up all the words
		/// </summary>
		public string Letters
		{
			get
			{
				if (!isLoaded)
				{
					ParseLevelFile();
				}

				return letters;
			}
		}

		/// <summary>
		/// Gets the words that must be found for this level
		/// </summary>
		public List<WordData> Words
		{
			get
			{
				if (!isLoaded)
				{
					ParseLevelFile();
				}

				return words;
			}
		}

		/// <summary>
		/// Gets the number of rows on the board for this level (Only used with Grid type levels)
		/// </summary>
		public int BoardRows
		{
			get
			{
				if (!isLoaded)
				{
					ParseLevelFile();
				}

				return boardRows;
			}
		}

		/// <summary>
		/// Gets the number of columns on the board for this level (Only used with Grid type levels)
		/// </summary>
		public int BoardCols
		{
			get
			{
				if (!isLoaded)
				{
					ParseLevelFile();
				}

				return boardCols;
			}
		}

		public int PackIndex		{ get; set; }
		public int CategoryIndex	{ get; set; }
		public int LevelIndex		{ get; set; }

		/// <summary>
		/// Gets or sets the level number for this level with respect to all other levels in the game
		/// </summary>
		public int GameLevelNumber { get; set; }

		/// <summary>
		/// Gets or sets the level number for this level with respect to the pack it belongs to
		/// </summary>
		public int PackLevelNumber { get; set; }

		/// <summary>∫
		/// Gets or sets the level number for this level with respect to the category it belongs to
		/// </summary>
		public int CategoryLevelNumber { get; set; }

		#endregion

		#region Public Methods

		public LevelData(string contents)
		{
			levelFileContents = contents;
		}

		/// <summary>
		/// Gets the LevelWordData for the given string word
		/// </summary>
		public WordData GetLevelWordData(string word)
		{
			for (int i = 0; i < Words.Count; i++)
			{
				WordData levelWordData = Words[i];

				if (word.ToLower() == levelWordData.Word.ToLower())
				{
					return levelWordData;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the LevelWordData (If any) that is in the given direction and has a letter at the given rIndex, cIndex
		/// </summary>
		public WordData GetLevelWordData(bool isVertical, int rIndex, int cIndex)
		{
			for (int i = 0; i < Words.Count; i++)
			{
				WordData levelWordData = Words[i];

				// Check if the word is in the correct direction
				if (isVertical == levelWordData.IsVerticalOnBoard)
				{
					if (rIndex >= levelWordData.BoardRowStartIndex && rIndex <= levelWordData.BoardRowEndIndex &&
					    cIndex >= levelWordData.BoardColStartIndex && cIndex <= levelWordData.BoardColEndIndex)
					{
						return levelWordData;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the first WordData that uses the given row/col as one of its letters, sets letterIndex to the index into the word for the letter.
		/// </summary>
		public WordData GetLevelWordData(int row, int col, out int letterIndex)
		{
			letterIndex = 0;

			for (int j = 0; j < Words.Count; j++)
			{
				WordData wordData = Words[j];

				// Check if the row/col is part of this word
				if (row >= wordData.BoardRowStartIndex && row <= wordData.BoardRowEndIndex &&
				    col >= wordData.BoardColStartIndex && col <= wordData.BoardColEndIndex)
				{
					// Get the character index in the word for the row/col
					if (wordData.IsVerticalOnBoard)
					{
						letterIndex = row - wordData.BoardRowStartIndex;
					}
					else
					{
						letterIndex = col - wordData.BoardColStartIndex;
					}

					return wordData;
				}
			}

			return null;
		}

		#endregion

		#region Private Methods

		private void ParseLevelFile()
		{
			isLoaded = true;

			JSONNode levelJson = JSON.Parse(levelFileContents);

			id			= levelJson["id"].Value;
			levelType	= (Type)levelJson["type"].AsInt;
			letters		= levelJson["letters"].Value;

			words = new List<WordData>();

			foreach (JSONNode wordJson in levelJson["words"].AsArray)
			{
				words.Add(new WordData(wordJson, levelType));
			}

			// Sort the words by length then alphabetically
			words.Sort((WordData x, WordData y) =>
			{
				int diff = x.Word.Length - y.Word.Length;

				if (diff == 0)
				{
					return string.Compare(x.Word, y.Word);
				}

				return diff;
			});

			if (levelType == Type.Grid)
			{
				boardRows = levelJson["rowCount"].AsInt;
				boardCols = levelJson["colCount"].AsInt;
			}
		}

		#endregion
	}

	public class WordData
	{
		#region Properties

		public string		Word				{ get; private set; }
		public bool			AwardCoins			{ get; private set; }
		public bool			IsVerticalOnBoard	{ get; private set; }
		public int			BoardRowStartIndex	{ get; private set; }
		public int			BoardColStartIndex	{ get; private set; }
		public int			BoardRowEndIndex	{ get; private set; }
		public int			BoardColEndIndex	{ get; private set; }
		public List<int>	CoinIndices			{ get; private set; }

		#endregion

		#region Public Methods

		public WordData(JSONNode levelWordJson, LevelData.Type levelType)
		{
			Word		= levelWordJson["word"].Value;
			AwardCoins	= levelWordJson["awardCoins"].AsBool;

			if (levelType == LevelData.Type.Grid)
			{
				IsVerticalOnBoard	= levelWordJson["vertical"].AsBool;
				BoardRowStartIndex	= levelWordJson["rowIndex"].AsInt;
				BoardColStartIndex	= levelWordJson["colIndex"].AsInt;
				BoardRowEndIndex	= BoardRowStartIndex + (IsVerticalOnBoard ? Word.Length - 1 : 0);
				BoardColEndIndex	= BoardColStartIndex + (IsVerticalOnBoard ? 0 : Word.Length - 1);

				CoinIndices = new List<int>();

				foreach (JSONNode coinIndiceJson in levelWordJson["coinIndices"].AsArray)
				{
					CoinIndices.Add(coinIndiceJson.AsInt);
				}
			}
		}

		#endregion
	}
}
