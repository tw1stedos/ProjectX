using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using dotmob;

namespace WordCross
{
	public class LevelSaveData
	{
		#region Member Variables

		public List<string>						foundWords		= new List<string>();
		public Dictionary<string, List<bool>>	hintIndicesUsed	= new Dictionary<string, List<bool>>();
		public List<List<char>>					characterGrid	= new List<List<char>>();
		public int								extraWords		= 0;

		#endregion

		#region Public Methods

		public LevelSaveData(LevelData levelData)
		{
			Initialize(levelData);
		}

		public LevelSaveData(JSONNode savedData)
		{
			LoadSave(savedData);
		}

		public object Save()
		{
			Dictionary<string, object> json = new Dictionary<string, object>();

			json["found_words"]		= foundWords;
			json["hint_indices"]	= SaveHintIndices();
			json["character_grid"]	= SaveCharacterGrid();
			json["extra_words"]		= extraWords;

			return json;
		}

		public void LoadSave(JSONNode json)
		{
			LoadSavedFoundWords(json["found_words"].AsArray);
			LoadSavedHintIndices(json["hint_indices"].AsArray);
			LoadSavedCharacterGrid(json["character_grid"].AsArray);

			extraWords = json["extra_words"].AsInt;
		}

		public bool HasChanged()
		{
			// Check for any words or extra words have been found
			if (foundWords.Count > 0 || extraWords > 0)
			{
				return true;
			}

			// Check for any true hint indicies meaning a hint was used in the level
			foreach (KeyValuePair<string, List<bool>> pair in hintIndicesUsed)
			{
				for (int i = 0; i < pair.Value.Count; i++)
				{
					if (pair.Value[i])
					{
						return true;
					}
				}
			}

			// Nothing has changed in the level since starting it
			return false;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Initialize this instance.
		/// </summary>
		private void Initialize(LevelData levelData)
		{
			InitializeHintIndices(levelData);

			if (levelData.LevelType == LevelData.Type.Grid)
			{
				InitializeCharacterGrid(levelData);
			}
		}

		/// <summary>
		/// Initializes the character grid for the given level data
		/// </summary>
		private void InitializeCharacterGrid(LevelData levelData)
		{
			// First create a blank board
			for (int i = 0; i < levelData.BoardRows; i++)
			{
				List<char> characterGridRow = new List<char>();

				for (int j = 0; j < levelData.BoardCols; j++)
				{
					characterGridRow.Add((char)0);
				}

				characterGrid.Add(characterGridRow);
			}

			// Now for each word in the level data, add a space character to indicate a character goes there
			for (int i = 0; i < levelData.Words.Count; i++)
			{
				WordData wordData = levelData.Words[i];

				int rStart = wordData.BoardRowStartIndex;
				int cStart = wordData.BoardColStartIndex;
				int rEnd = wordData.BoardRowEndIndex;
				int cEnd = wordData.BoardColEndIndex;
				int rInc = wordData.IsVerticalOnBoard ? 1 : 0;
				int cInc = wordData.IsVerticalOnBoard ? 0 : 1;

				for (int r = rStart, c = cStart; r <= rEnd && c <= cEnd; r += rInc, c += cInc)
				{
					characterGrid[r][c] = ' ';
				}
			}
		}

		/// <summary>
		/// Initializes the list of hint indices
		/// </summary>
		private void InitializeHintIndices(LevelData levelData)
		{
			for (int i = 0; i < levelData.Words.Count; i++)
			{
				string word = levelData.Words[i].Word;

				List<bool> list = new List<bool>();

				for (int j = 0; j < word.Length; j++)
				{
					list.Add(false);
				}

				hintIndicesUsed.Add(word, list);
			}
		}

		/// <summary>
		/// Saves the hint indices as a list so it can be converted to json
		/// </summary>
		private List<object> SaveHintIndices()
		{
			List<object> hintIndicesJson = new List<object>();

			foreach (KeyValuePair<string, List<bool>> pair in hintIndicesUsed)
			{
				Dictionary<string, object> hintIndiceJson = new Dictionary<string, object>();

				hintIndiceJson["key"]	= pair.Key;
				hintIndiceJson["value"]	= pair.Value;

				hintIndicesJson.Add(hintIndiceJson);
			}

			return hintIndicesJson;
		}

		/// <summary>
		/// Saves the character grid chars as a list of list of ints
		/// </summary>
		private List<List<object>> SaveCharacterGrid()
		{
			List<List<object>> characterGridJson = new List<List<object>>();

			for (int i = 0; i < characterGrid.Count; i++)
			{
				characterGridJson.Add(new List<object>());

				for (int j = 0; j < characterGrid[i].Count; j++)
				{
					characterGridJson[characterGridJson.Count - 1].Add((int)characterGrid[i][j]);
				}
			}

			return characterGridJson;
		}

		/// <summary>
		/// Loads the saved list of found words
		/// </summary>
		private void LoadSavedFoundWords(JSONArray foundWordsJson)
		{
			foundWords.Clear();

			foreach (JSONNode foundWord in foundWordsJson)
			{
				foundWords.Add(foundWord.Value);
			}
		}

		/// <summary>
		/// Loads dictionary of used hints
		/// </summary>
		private void LoadSavedHintIndices(JSONArray hintIndicesJson)
		{
			hintIndicesUsed.Clear();

			foreach (JSONNode hintIndiceJson in hintIndicesJson)
			{
				string	key		= hintIndiceJson["key"].Value;
				int		value	= hintIndiceJson["value"].AsInt;

				List<bool> usedHints = new List<bool>();

				foreach (JSONNode node in hintIndiceJson["value"].AsArray)
				{
					usedHints.Add(node.AsBool);
				}

				hintIndicesUsed.Add(key, usedHints);
			}
		}

		/// <summary>
		/// Loads the character grid if the level is a grid level
		/// </summary>
		private void LoadSavedCharacterGrid(JSONArray characterGridJson)
		{
			characterGrid.Clear();

			foreach (JSONArray characterGridRowJson in characterGridJson)
			{
				characterGrid.Add(new List<char>());

				foreach (JSONNode characterGridCell in characterGridRowJson)
				{
					characterGrid[characterGrid.Count - 1].Add((char)characterGridCell.AsInt);
				}
			}
		}

		#endregion
	}
}
