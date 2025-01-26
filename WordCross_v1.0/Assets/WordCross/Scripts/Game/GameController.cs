using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using dotmob;

namespace WordCross
{
	public class GameController : SingletonComponent<GameController>, ISaveable
	{
		#region Inspector Variables

		[SerializeField] private WordBoardGrid		wordBoardGrid		= null;
		[SerializeField] private WordBoardList		wordBoardList		= null;
		[SerializeField] private LetterWheel		letterWheel			= null;
		[SerializeField] private SelectedLetters	selectedLetters		= null;
		[SerializeField] private ExtraWords			extraWords			= null;
		[Space]
		[SerializeField] private string			saveFileName				= "save";
		[SerializeField] private TextAsset		wordFile					= null;
		[SerializeField] private int			coinsToStart				= 500;
		[SerializeField] private int			coinsMultiplier				= 1;
		[SerializeField] private int			coinCostPerHint				= 10;
		[SerializeField] private int			coinCostPerTargetHint		= 25;
		[SerializeField] private int			coinCostPerMultiHint		= 50;
		[SerializeField] private int			numToShowForMultiHint		= 5;
		[SerializeField] private int			numLevelsTillAd				= 3;
		[SerializeField] private bool			debugDisableLocking			= false;
		[Space]
		[SerializeField][HideInInspector] private List<PackInfo> packInfos = null;

		#endregion

		#region Member Variables

		// 1 - Initial version
		// 2 - Removed completed value from LevelSave Data. Added LastCompletedLevelNumber. No longer saving LevelSaveDatas unless something needs to be saved.
		private const int SaveVersion = 2;

		private Dictionary<string, LevelSaveData>	levelSaveDatas;
		private LoadExtraWordsWorker				loadExtraWordsWorker;
		private HashSet<string>						allWords;

		#endregion

		#region Properties

		/// <summary>
		/// The old save file location, asset version 1.4 and up now use SaveManager
		/// </summary>
		public string SaveFilePath	{ get { return Application.persistentDataPath + string.Format("/{0}.json", saveFileName); } }

		public string			SaveId						{ get { return saveFileName; } }
		public List<PackInfo>	PackInfos					{ get { return packInfos; } }
		public int				CoinCostPerHint				{ get { return coinCostPerHint; } }
		public int				CoinCostPerTargetHint		{ get { return coinCostPerTargetHint; } }
		public int				CoinCostPerMultiHint		{ get { return coinCostPerMultiHint; } }
		public ActiveLevel		CurrentActiveLevel			{ get; private set; }
		public int				GamePoints					{ get; private set; }
		public int				Coins						{ get; private set; }
		public int				NumLevelsTillAdShows		{ get; private set; }
		public bool				PlayerSelectingHint			{ get; private set; }
		public int				LastCompletedLevelNumber	{ get; private set; }

		public bool DebugDisableLocking
		{
			get
			{
				#if !UNITY_EDITOR
				return false;
				#else
				return debugDisableLocking;
				#endif
			}
		}

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			SaveManager.Instance.Register(this);

			// Initialie the word boards
			if (wordBoardGrid != null)
			{
				wordBoardGrid.Initialize();
				wordBoardGrid.OnWordBoardCellClicked += OnWordGridCellClicked;
			}

			if (wordBoardList != null)
			{
				wordBoardList.Initialize();
				wordBoardList.OnWordBoardCellClicked += OnWordGridCellClicked;
			}

			letterWheel.Initialize();

			letterWheel.OnWordSelected				+= OnWordSelected;
			letterWheel.OnSelectedLettersUpdated	+= OnSelectedLettersUpdated;

			// Create the LevelDatas and load all the level files in each category
			CreateCategoryLevelDatas();

			// Load the save file
			if (!LoadSave())
			{
				// If no save file exists then set the starting values
				Coins						= coinsToStart;
				LastCompletedLevelNumber	= 0;
			}

			CoinController.Instance.SetCoinsText(Coins);

			// Loads the word file to be used to check for extra words
			LoadWordFile();
		}

		private void Update()
		{
			if (loadExtraWordsWorker != null && loadExtraWordsWorker.Stopped)
			{
				allWords = loadExtraWordsWorker.allWords;
				loadExtraWordsWorker = null;
			}
		}

		private void OnDestroy()
		{
			Save();
		}

		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				Save();
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Creates a new current active level and starts it
		/// </summary>
		public void StartLevel(int gameLevelNumber)
		{
			for (int i = 0; i < packInfos.Count; i++)
			{
				PackInfo packInfo = PackInfos[i];

				for (int j = 0; j < packInfo.categoryInfos.Count; j++)
				{
					CategoryInfo categoryInfo = packInfo.categoryInfos[j];

					if (categoryInfo.levelFiles.Count == 0 || categoryInfo.LevelDatas[categoryInfo.LevelDatas.Count - 1].GameLevelNumber < gameLevelNumber)
					{
						continue;
					}

					int levelIndex = gameLevelNumber - categoryInfo.LevelDatas[0].GameLevelNumber;

					StartLevel(packInfo, categoryInfo, categoryInfo.LevelDatas[levelIndex]);

					return;
				}
			}
		}

		/// <summary>
		/// Creates a new current active level and starts it
		/// </summary>
		public void StartLevel(PackInfo packInfo, CategoryInfo categoryInfo, int levelIndex)
		{
			if (levelIndex < categoryInfo.LevelDatas.Count)
			{
				StartLevel(packInfo, categoryInfo, categoryInfo.LevelDatas[levelIndex]);
			}
		}

		/// <summary>
		/// Creates a new current active level and starts it
		/// </summary>
		public void StartLevel(PackInfo packInfo, CategoryInfo categoryInfo, LevelData levelData)
		{
			ActiveLevel activeLevel = new ActiveLevel();

			activeLevel.packInfo		= packInfo;
			activeLevel.categoryInfo	= categoryInfo;
			activeLevel.levelData		= levelData;
			activeLevel.levelSaveData	= GetLevelSaveData(levelData.Id);

			// If there is no save data then create a new LevelSaveData for this level
			if (activeLevel.levelSaveData == null)
			{
				activeLevel.levelSaveData = new LevelSaveData(activeLevel.levelData);

				// Set the new LevelSaveData in the dictionary so it is saved in the game save file
				levelSaveDatas[activeLevel.levelData.Id] = activeLevel.levelSaveData;
			}

			// Check if we should show an ad
			if (NumLevelsTillAdShows <= 0)
			{
               
                if (Advertisements.Instance.IsInterstitialAvailable())
                {
					Advertisements.Instance.ShowInterstitial();
					NumLevelsTillAdShows = numLevelsTillAd;
				}
			}

			// Decrease the number of levels until an ad shows
			NumLevelsTillAdShows--;

			StartLevel(activeLevel);
		}

		/// <summary>
		/// SHows a hint in the current active level
		/// </summary>
		public void ShowHint()
		{
			if (CurrentActiveLevel == null)
			{
				return;
			}

			if (Coins < CoinCostPerHint)
			{
				PopupManager.Instance.Show("not_enough_coins");
			}
			else
			{
				Coins -= CoinCostPerHint;

				CoinController.Instance.SetCoinsText(Coins);

				ShowHint(CurrentActiveLevel);

				SoundManager.Instance.Play("hint-used");
			}
		}

		/// <summary>
		/// Hows multiple hints for the current active level
		/// </summary>
		public void ShowMultiHint()
		{
			if (CurrentActiveLevel == null)
			{
				return;
			}

			if (Coins < CoinCostPerMultiHint)
			{
				PopupManager.Instance.Show("not_enough_coins");
			}
			else
			{
				Coins -= CoinCostPerMultiHint;

				CoinController.Instance.SetCoinsText(Coins);

				ShowMultiHint(CurrentActiveLevel, numToShowForMultiHint);

				SoundManager.Instance.Play("hint-used");
			}
		}

		/// <summary>
		/// Starts the mode for the user selecting what letter they want to show
		/// </summary>
		public void TogglePlayerSelectingHint()
		{
			if (CurrentActiveLevel == null)
			{
				return;
			}

			if (PlayerSelectingHint)
			{
				EndPlayerSelectingHint();
			}
			else if (Coins < CoinCostPerTargetHint)
			{
				PopupManager.Instance.Show("not_enough_coins");
			}
			else
			{
				PlayerSelectingHint = true;

				UIController.Instance.UpdatePlayerSelectingHint();
			}
		}

		/// <summary>
		/// Ends the mode for the user selecting what letter they want to show
		/// </summary>
		public void EndPlayerSelectingHint()
		{
			if (PlayerSelectingHint)
			{
				PlayerSelectingHint = false;

				UIController.Instance.UpdatePlayerSelectingHint();
			}
		}

		/// <summary>
		/// Gets the LevelData for the given pack/category/level
		/// </summary>
		public LevelData GetLevelData(int packIndex, int categoryIndex, int levelIndex)
		{
			if (packIndex < PackInfos.Count)
			{
				PackInfo packInfo = PackInfos[packIndex];

				if (categoryIndex < packInfo.categoryInfos.Count)
				{
					CategoryInfo categoryInfo = packInfo.categoryInfos[categoryIndex];

					if (levelIndex < categoryInfo.LevelDatas.Count)
					{
						return categoryInfo.LevelDatas[levelIndex];
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the LevelSaveData for the given level id
		/// </summary>
		public LevelSaveData GetLevelSaveData(string levelId)
		{
			return levelSaveDatas.ContainsKey(levelId) ? levelSaveDatas[levelId] : null;
		}

		/// <summary>
		/// Gives the coins.
		/// </summary>
		public void GiveCoins(int coins, bool applyMultiplier = true)
		{
			if (applyMultiplier)
			{
				coins *= coinsMultiplier;
			}

			Coins += coins;
		}

		/// <summary>
		/// Gives the coins.
		/// </summary>
		public void GiveCoinsIAP(int coins)
		{
			Coins += coins;

			CoinController.Instance.SetCoinsText(Coins);
		}

		public bool IsLevelLocked(LevelData levelData)
		{
			// The level is locked if it's game level number is greater than the next level after the last completed level
			return !GameController.Instance.DebugDisableLocking && levelData.GameLevelNumber > LastCompletedLevelNumber + 1;
		}

		public bool IsCategoryLocked(CategoryInfo categoryInfo)
		{
			// The category is locked if the first level in the category is locked
			return categoryInfo.LevelDatas.Count > 0 && IsLevelLocked(categoryInfo.LevelDatas[0]);
		}

		public bool IsPackLocked(PackInfo packInfo)
		{
			// The pack is locked if the first category is locked
			return packInfo.categoryInfos.Count > 0 && IsCategoryLocked(packInfo.categoryInfos[0]);
		}

		public bool IsLevelCompleted(LevelData levelData)
		{
			// Check if the levels game level number is greater than or equal to the last completed level number
			return levelData.GameLevelNumber <= LastCompletedLevelNumber;
		}

		public bool IsCategoryCompleted(CategoryInfo categoryInfo)
		{
			// Check if the last level in the category is completed, if so then the whole category is completed
			return categoryInfo.LevelDatas.Count > 0 && IsLevelCompleted(categoryInfo.LevelDatas[categoryInfo.LevelDatas.Count - 1]);
		}

		public bool IsPackCompleted(PackInfo packInfo)
		{
			// Check if the last category in the pack is completed, if so then the whole pack is completed
			return packInfo.categoryInfos.Count > 0 && IsCategoryCompleted(packInfo.categoryInfos[packInfo.categoryInfos.Count - 1]);
		}

		public bool IsLastLevelInGameCompleted()
		{
			// Check if the last pack is completed, if so then the last level in the game has been completed
			return PackInfos.Count > 0 && IsPackCompleted(PackInfos[PackInfos.Count - 1]);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates all the LevelDatas
		/// </summary>
		private void CreateCategoryLevelDatas()
		{
			int	gameLevelNumber	= 1;

			// Loop through all the packs
			for (int i = 0; i < PackInfos.Count; i++)
			{
				PackInfo	packInfo		= PackInfos[i];
				int			packLevelNumber	= 1;

				// Loop through all the categoies in the pack
				for (int j = 0; j < packInfo.categoryInfos.Count; j++)
				{
					CategoryInfo	categoryInfo		= packInfo.categoryInfos[j];
					int				categoryLevelNumber	= 1;

					categoryInfo.LevelDatas = new List<LevelData>();

					// Loop through all the levels in the category
					for (int k = 0; k < categoryInfo.levelFiles.Count; k++)
					{
						if (categoryInfo.levelFiles[k] == null)
						{
							Debug.Log("Null level file in category: " + categoryInfo.displayName);
						}

						LevelData levelData = new LevelData(categoryInfo.levelFiles[k].text);

						levelData.PackIndex		= i;
						levelData.CategoryIndex	= j;
						levelData.LevelIndex	= k;

						levelData.GameLevelNumber		= gameLevelNumber++;
						levelData.PackLevelNumber		= packLevelNumber++;
						levelData.CategoryLevelNumber	= categoryLevelNumber++;

						categoryInfo.LevelDatas.Add(levelData);
					}
				}
			}
		}

		/// <summary>
		/// Starts a level
		/// </summary>
		private void StartLevel(ActiveLevel level)
		{
			CurrentActiveLevel = level;

			switch (CurrentActiveLevel.levelData.LevelType)
			{
				case LevelData.Type.Grid:
					wordBoardGrid.Setup(CurrentActiveLevel);

					if (wordBoardList != null)
					{
						wordBoardList.Clear();
					}
					break;
				case LevelData.Type.List:
					wordBoardList.Setup(CurrentActiveLevel);

					if (wordBoardGrid != null)
					{
						wordBoardGrid.Clear();
					}
					break;
			}

			extraWords.SetNumExtraWordsFound(CurrentActiveLevel.levelSaveData.extraWords);

			letterWheel.Setup(CurrentActiveLevel);

			UIController.Instance.OnNewLevelStarted();
		}

		/// <summary>
		/// Called when the LetterWheel selects a word
		/// </summary>
		private void OnWordSelected(string word)
		{
			// Try and get the WordData for the selected word for the current level
			WordData levelWordData = CurrentActiveLevel.levelData.GetLevelWordData(word);

			// Check if they already found the word
			if (CurrentActiveLevel.levelSaveData.foundWords.Contains(word))
			{
				// Set the selected letters as already found
				selectedLetters.SetAlreadyFound();

				// If levelWordData is not null then shake the word on the board to indicate to the player they already found the word
				if (levelWordData != null)
				{
					switch (CurrentActiveLevel.levelData.LevelType)
					{
						case LevelData.Type.Grid:
							wordBoardGrid.ShakeWord(levelWordData);
							break;
						case LevelData.Type.List:
							wordBoardList.ShakeWord(levelWordData);
							break;
					}
				}
				// Else the found word was an extra word so shake the extra word container
				else
				{
					extraWords.Shake();
				}

				SoundManager.Instance.Play("word-already-found");

				return;
			}

			// If the word data is not null then the player has found a word in the level
			if (levelWordData != null)
			{
				// Set the word as found
				FoundWord(CurrentActiveLevel, levelWordData);

				// Set the current selected word as correct
				selectedLetters.SetCorrect();

				SoundManager.Instance.Play("word-correct");
			}
			// If allWords contains the word then the word is a valid word it's just not part of the current level so the player found an extra word
			else if (allWords != null && allWords.Contains(word.ToLower()))
			{
				// Set the word as a found extra word
				FoundExtraWord(CurrentActiveLevel, word);

				// Set the current selected word as an extra word
				selectedLetters.SetExtraWord(extraWords.transform as RectTransform);

				SoundManager.Instance.Play("word-extra");
			}
			else
			{
				// The word is not a valid word so set the selected letters as in-correct
				selectedLetters.SetWrong();

				SoundManager.Instance.Play("word-invalid");
			}
		}

		/// <summary>
		/// Called when the LetterWheel selects another letter
		/// </summary>
		private void OnSelectedLettersUpdated(string letters)
		{
			selectedLetters.SetSelectedLetters(letters, CurrentActiveLevel.packInfo.color);
		}

		/// <summary>
		/// Called when a word for the ActiveLevel has been found
		/// </summary>
		private void FoundWord(ActiveLevel level, WordData levelWordData)
		{
			// Add the word to the list of found words
			level.levelSaveData.foundWords.Add(levelWordData.Word);

			// If the level type is grid then check for words that have been revialed because all cells that a letter in them
			if (level.levelData.LevelType == LevelData.Type.Grid)
			{
				PlaceWordOnCharacterGrid(level, levelWordData);

				// Check for any newly found words that cross the word that was just found
				List<WordData> newFoundWords = CheckForFoundWords(level, levelWordData);

				// Make sure all the newly found words are shown as found on the word board
				ShowWordsOnBoard(level, newFoundWords);
			}

			// Show the word on the WordBoard
			ShowWordOnBoard(level, levelWordData);

			// Award any coins that can be awarded
			//AwardCoins(level, levelWordData);

			// Check if the level is complete
			if (IsBoardComplete(level))
			{
				CompleteLevel(level);
			}
		}

		/// <summary>
		/// Shows the given word on the correct word board
		/// </summary>
		private void ShowWordOnBoard(ActiveLevel level, WordData levelWordData)
		{
			// Show the word on the WordBoard
			switch (level.levelData.LevelType)
			{
				case LevelData.Type.Grid:
					wordBoardGrid.ShowWord(levelWordData);
					break;
				case LevelData.Type.List:
					wordBoardList.ShowWord(levelWordData);
					break;
			}
		}

		/// <summary>
		/// Shows all the given words on the correct word board
		/// </summary>
		private void ShowWordsOnBoard(ActiveLevel level, List<WordData> levelWordDatas)
		{
			for (int i = 0; i < levelWordDatas.Count; i++)
			{
				ShowWordOnBoard(level, levelWordDatas[i]);
			}
		}

		/// <summary>
		/// Called when a word has been found that exists in the word file but is not part of the current level
		/// </summary>
		private void FoundExtraWord(ActiveLevel level, string word)
		{
			level.levelSaveData.extraWords++;
			level.levelSaveData.foundWords.Add(word);

			extraWords.SetNumExtraWordsFound(level.levelSaveData.extraWords);
		}

		/// <summary>
		/// Places the given word on the levels character grid
		/// </summary>
		private void PlaceWordOnCharacterGrid(ActiveLevel level, WordData levelWordData)
		{
			for (int i = 0; i < levelWordData.Word.Length; i++)
			{
				PlaceLetterOnCharacterGrid(level, levelWordData, i);
			}
		}

		/// <summary>
		/// Places the given letter on the levels character grid
		/// </summary>
		private void PlaceLetterOnCharacterGrid(ActiveLevel level, WordData levelWordData, int letterIndex)
		{
			int rowIndex = levelWordData.BoardRowStartIndex + (levelWordData.IsVerticalOnBoard ? letterIndex : 0);
			int colIndex = levelWordData.BoardColStartIndex + (levelWordData.IsVerticalOnBoard ? 0 : letterIndex);

			level.levelSaveData.characterGrid[rowIndex][colIndex] = levelWordData.Word[letterIndex];
		}

		/// <summary>
		/// Checks if any words that cross the given word are now found
		/// </summary>
		private List<WordData> CheckForFoundWords(ActiveLevel level, WordData levelWordData)
		{
			List<WordData> newFoundWords = new List<WordData>();

			for (int i = 0; i < levelWordData.Word.Length; i++)
			{
				int rowIndex = levelWordData.BoardRowStartIndex + (levelWordData.IsVerticalOnBoard ? i : 0);
				int colIndex = levelWordData.BoardColStartIndex + (levelWordData.IsVerticalOnBoard ? 0 : i);

				WordData crossingWordData = CurrentActiveLevel.levelData.GetLevelWordData(!levelWordData.IsVerticalOnBoard, rowIndex, colIndex);

				// Check if there is a crosswing word and it has not been found yet
				if (crossingWordData != null && !level.levelSaveData.foundWords.Contains(crossingWordData.Word))
				{
					// Check if this word is now found
					if (CheckIsFoundWord(level, crossingWordData))
					{
						newFoundWords.Add(crossingWordData);
					}
				}
			}

			return newFoundWords;
		}

		/// <summary>
		/// Checks if the given word has all of its letters on the character grid and if so adds the word to the list of found words
		/// </summary>
		private bool CheckIsFoundWord(ActiveLevel level, WordData levelWordData)
		{
			for (int i = 0; i < levelWordData.Word.Length; i++)
			{
				int rowIndex = levelWordData.BoardRowStartIndex + (levelWordData.IsVerticalOnBoard ? i : 0);
				int colIndex = levelWordData.BoardColStartIndex + (levelWordData.IsVerticalOnBoard ? 0 : i);

				if (level.levelSaveData.characterGrid[rowIndex][colIndex] == ' ')
				{
					// There is still a blank cell so it has not been found yet
					return false;
				}
			}

			// If we get there then non of the letters on the character grid where blank for this word, so set it as found
			level.levelSaveData.foundWords.Add(levelWordData.Word);

			return true;
		}

		/// <summary>
		/// Returns true if all words have been found
		/// </summary>
		private bool IsBoardComplete(ActiveLevel level)
		{
			for (int i = 0; i < level.levelData.Words.Count; i++)
			{
				if (!level.levelSaveData.foundWords.Contains(level.levelData.Words[i].Word))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Shows a single letter in a word for the given level
		/// </summary>
		private void ShowHint(ActiveLevel level)
		{
			WordData	hintWordData	= null;
			int			hintIndex		= int.MaxValue;

			for (int i = 0; i < level.levelData.Words.Count; i++)
			{
				WordData	levelWordData = level.levelData.Words[i];
				int			wordHintIndex = GetHintIndex(level, levelWordData);

				if (wordHintIndex != -1 && wordHintIndex < hintIndex)
				{
					hintWordData	= levelWordData;
					hintIndex		= wordHintIndex;
				}
			}

			// Check if there is any word that can show a hint
			if (hintWordData != null)
			{
				ShowLetterForHint(level, hintWordData, hintIndex);
			}
		}

		/// <summary>
		/// Returns the character index for the next letter that should be shown as a hint for the given word.
		/// Returns -1 if there are no letters to show.
		/// </summary>
		private int GetHintIndex(ActiveLevel level, WordData levelWordData)
		{
			// If the word has been found then it should not show any hints
			if (level.levelSaveData.foundWords.Contains(levelWordData.Word))
			{
				return -1;
			}

			// Get the index for the next hint to show
			int hintIndex = 0;

			if (level.levelSaveData.hintIndicesUsed.ContainsKey(levelWordData.Word))
			{
				// Set to -1 so after we know if we found a hint or not
				hintIndex = -1;

				List<bool> hintIndiciesUsed = level.levelSaveData.hintIndicesUsed[levelWordData.Word];

				for (int i = 0; i < hintIndiciesUsed.Count; i++)
				{
					bool used = hintIndiciesUsed[i];

					if (!hintIndiciesUsed[i])
					{
						hintIndex = i;

						break;
					}
				}

				// If hint index is still -1 then a hint has been shown for every letter in this word
				if (hintIndex == -1)
				{
					// Return -1 to indicate no hint index could be found
					return -1;
				}
			}

			// If the level is a grid type then check that the next hint index does not already have a letter on it and if it does
			// get the next index that doesn't have a letter on it
			if (level.levelData.LevelType == LevelData.Type.Grid)
			{
				for (int i = hintIndex; i < levelWordData.Word.Length; i++)
				{
					int rowIndex = levelWordData.BoardRowStartIndex + (levelWordData.IsVerticalOnBoard ? i : 0);
					int colIndex = levelWordData.BoardColStartIndex + (levelWordData.IsVerticalOnBoard ? 0 : i);

					if (level.levelSaveData.characterGrid[rowIndex][colIndex] == ' ')
					{
						break;
					}

					hintIndex++;
				}
			}

			// If the hint index is now equal to the length then there is no possible hint we can show because all letters have been shown
			if (hintIndex >= levelWordData.Word.Length)
			{
				return -1;
			}

			return hintIndex;
		}

		/// <summary>
		/// Shows a bunch of hints randomly
		/// </summary>
		private void ShowMultiHint(ActiveLevel level, int numberOfHints)
		{
			switch (level.levelData.LevelType)
			{
				case LevelData.Type.Grid:
					ShowMultiHintForGrid(level, numberOfHints);
					break;
				case LevelData.Type.List:
					ShowMultiHintForList(level, numberOfHints);
					break;
			}
		}

		/// <summary>
		/// Shows a bunch of hints randomly in the level assuming the level is a grid type
		/// </summary>
		private void ShowMultiHintForGrid(ActiveLevel level, int numberOfHints)
		{
			List<int[]> emptyCells = new List<int[]>();

			// Get all the empty cells on the grid
			for (int row = 0; row < level.levelSaveData.characterGrid.Count; row++)
			{
				List<char> gridRow = level.levelSaveData.characterGrid[row];

				for (int col = 0; col < gridRow.Count; col++)
				{
					char gridCharacter = gridRow[col];

					// If the character is a space than that means this cell has not be shown yet
					if (gridCharacter == ' ')
					{
						emptyCells.Add(new int[] { row, col });
					}
				}
			}

			// Show the requested number of hints
			for (int i = 0; i < numberOfHints && emptyCells.Count > 0; i++)
			{
				// Pick a random empty cell
				int randIndex	= Random.Range(0, emptyCells.Count);
				int row			= emptyCells[randIndex][0];
				int col			= emptyCells[randIndex][1];

				// Remove it so it's not picked again
				emptyCells.RemoveAt(randIndex);

				// Get any WordData that this cell belongs to
				int			letterIndex	= 0;
				WordData	wordData	= level.levelData.GetLevelWordData(row, col, out letterIndex);

				// Finally show the letter on the grid
				ShowLetterForHint(level, wordData, letterIndex);
			}
		}

		/// <summary>
		/// Shows a bunch of hints randomly in the level assuming the level is a list type
		/// </summary>
		private void ShowMultiHintForList(ActiveLevel level, int numberOfHints)
		{
			List<object[]> unusedHints = new List<object[]>();

			// Get all the unused hint indices in all the unfound words
			for (int i = 0; i < level.levelData.Words.Count; i++)
			{
				WordData wordData = level.levelData.Words[i];

				// Check if the word has not been found yet
				if (!level.levelSaveData.foundWords.Contains(wordData.Word))
				{
					List<int>	unusedHintIndices	= new List<int>();
					List<bool>	hintIndicesUsed		= level.levelSaveData.hintIndicesUsed[wordData.Word];

					for (int j = 0; j < hintIndicesUsed.Count; j++)
					{
						// If the indice has not been used as a hint yet then add it
						if (!hintIndicesUsed[j])
						{
							unusedHintIndices.Add(j);
						}
					}

					// If there are un-used hint indices then add the word
					if (unusedHintIndices.Count > 0)
					{
						object[] obj = new object[2];

						obj[0] = wordData;
						obj[1] = unusedHintIndices;

						unusedHints.Add(obj);
					}
				}
			}

			// Pick random hint indices to show hints on
			for (int i = 0; i < numberOfHints && unusedHints.Count > 0; i++)
			{
				// Get a random word to show a letter hint on
				int			randWordIndex	= Random.Range(0, unusedHints.Count);
				object[]	unusedHint		= unusedHints[randWordIndex];

				WordData	wordData			= unusedHint[0] as WordData;
				List<int>	unusedHintIndices	= unusedHint[1] as List<int>;

				// Get a random unused index to show the hint on
				int randHintIndex	= Random.Range(0, unusedHintIndices.Count);
				int letterIndex		= unusedHintIndices[randHintIndex];

				// Remove the hint index
				unusedHintIndices.RemoveAt(randHintIndex);

				if (unusedHintIndices.Count == 0)
				{
					unusedHints.RemoveAt(randWordIndex);
				}

				// Show the letter
				ShowLetterForHint(level, wordData, letterIndex);
			}
		}

		/// <summary>
		/// Sets a letter as shown and shows it on the WordBoard
		/// </summary>
		private void ShowLetterForHint(ActiveLevel level, WordData levelWordData, int letterIndex)
		{
			// Set the hint index as used
			level.levelSaveData.hintIndicesUsed[levelWordData.Word][letterIndex] = true;

			// If the level type is grid then place the hint letter on the character grid and check for completed words
			if (level.levelData.LevelType == LevelData.Type.Grid)
			{
				// Place the hint letter on the charachter grid
				PlaceLetterOnCharacterGrid(level, levelWordData, letterIndex);

				// Check if the word we placed a hint for is now found
				if (CheckIsFoundWord(level, levelWordData))
				{
					ShowWordOnBoard(level, levelWordData);
				}

				// Check for any new words that are found after placing the hint letter
				List<WordData> newFoundWords = CheckForFoundWords(level, levelWordData);

				ShowWordsOnBoard(level, newFoundWords);
			}
			// Else it's a List type level
			else
			{
				bool allLettersShown = true;

				// Check if all the letters of the word have been shown using hints
				for (int i = 0; i < levelWordData.Word.Length; i++)
				{
					if (!level.levelSaveData.hintIndicesUsed[levelWordData.Word][i])
					{
						allLettersShown = false;

						break;
					}
				}

				// If all the letters are shown set the word as found and show the word on the board
				if (allLettersShown)
				{
					level.levelSaveData.foundWords.Add(levelWordData.Word);

					ShowWordOnBoard(level, levelWordData);
				}
			}

			// Show the hint on the WordBoard
			switch (level.levelData.LevelType)
			{
				case LevelData.Type.Grid:
					wordBoardGrid.ShowLetterForHint(levelWordData, letterIndex);
					break;
				case LevelData.Type.List:
					wordBoardList.ShowLetterForHint(levelWordData, letterIndex);
					break;
			}

			// Check if the level is now complete after placing the hint
			if (IsBoardComplete(level))
			{
				CompleteLevel(level);
			}
		}

		/// <summary>
		/// Completes the given level
		/// </summary>
		private void CompleteLevel(ActiveLevel level)
		{
			Debug.LogFormat("[GameController] Level {0} complete", level.levelData.GameLevelNumber);

			SoundManager.Instance.Play("completed");

			bool	wasLevelCompleted	= level.levelData.GameLevelNumber <= LastCompletedLevelNumber;
			int		numExtraWordsFound	= level.levelSaveData.extraWords;

			// Set the last completed level number, make sure it's the max if the player replayed a level
			LastCompletedLevelNumber = Mathf.Max(LastCompletedLevelNumber, level.levelData.GameLevelNumber);

			// Remove the level save data since it's no longer needed (A new one will be created if the level is re-played)
			levelSaveDatas.Remove(level.levelData.Id);

			// Get the current game points
			int currentGamePoints = GamePoints;
			int gamePointsAwarded = 0;

			// Need to animate the number of coins from/to if the player completed a category
			int categoryCoinsAwarded	= 0;
			int categoryCoinsAmountFrom	= 0;
			int categoryCoinsAmountTo	= 0;

			// Need to animate the number of coins from/to if the player found extra words
			int extraWordsCoinsAwarded		= 0;
			int extraWordsCoinsAmountFrom	= 0;
			int extraWordsCoinsAmountTo		= 0;

			// Check if the level has not already been completed
			if (!wasLevelCompleted)
			{
				// Calculate the number of game points to award
				gamePointsAwarded = CalculateGamePointsAwardedForLevel(level);

				// Award the game points
				GamePoints += gamePointsAwarded;

				// Check if the category is now complete
				if (IsCategoryCompleted(level.categoryInfo))
				{
					// Get the number of coins to award and the current amount of coins
					categoryCoinsAwarded	= level.categoryInfo.coinsAwarded;
					categoryCoinsAmountFrom	= Coins;

					// Give the coins right away but don't update the text. This makes it so it the app exits the player has been given the coins
					// but we don't want to update teh text until the animation happens on the complete popup
					GiveCoins(categoryCoinsAwarded);

					// Get the amount of coins after the coins have been given
					categoryCoinsAmountTo = Coins;
				}

				// Award coins for each extra word the player found
				if (numExtraWordsFound > 0)
				{
					// Get the number of coins to award and the current amount of coins
					extraWordsCoinsAwarded		= numExtraWordsFound;
					extraWordsCoinsAmountFrom	= Coins;

					// Give the coins right away but don't update the text. This makes it so it the app exits the player has been given the coins
					// but we don't want to update teh text until the animation happens on the complete popup
					GiveCoins(extraWordsCoinsAwarded);

					// Get the amount of coins after the coins have been given
					extraWordsCoinsAmountTo = Coins;
				}
			}

			bool isLastLevel = IsLastLevel(level.levelData);

			// Add all the info the level complete popup needs
			object[] popupInData =
			{
				level,
				wasLevelCompleted,
				currentGamePoints,
				gamePointsAwarded,
				categoryCoinsAwarded,
				categoryCoinsAmountFrom,
				categoryCoinsAmountTo,
				extraWordsCoinsAwarded,
				extraWordsCoinsAmountFrom,
				extraWordsCoinsAmountTo,
				isLastLevel};

			StartCoroutine(ShowLevelCompletePopup(popupInData));
		}

		/// <summary>
		/// Waits a bit before showing the complete popup so word board animations can finish first
		/// </summary>
		private IEnumerator ShowLevelCompletePopup(object[] popupInData)
		{
			// Wait for the word that was just found to show on the board before showing the complete popup
			yield return new WaitForSeconds(0.5f);

			PopupManager.Instance.Show("level_complete", popupInData, OnLevelCompletePopupClosed);
		}

		/// <summary>
		/// Returns the amount of game points that should be awarded for completing the given level
		/// </summary>
		private int CalculateGamePointsAwardedForLevel(ActiveLevel level)
		{
			// Game points are calculated by multiplying the number of letters level by the sqrt of the level number
			int numberOfLetters		= level.levelData.Letters.Length;
			int sqrtOfLevelNumber	= Mathf.RoundToInt(Mathf.Sqrt(level.levelData.GameLevelNumber));

			return numberOfLetters * sqrtOfLevelNumber;
		}

		/// <summary>
		/// Checks if the given level is the last level in the game
		/// </summary>
		private bool IsLastLevel(LevelData levelData)
		{
			if (levelData.PackIndex == PackInfos.Count - 1)
			{
				List<CategoryInfo> categoryInfos = PackInfos[levelData.PackIndex].categoryInfos;

				if (levelData.CategoryIndex == categoryInfos.Count - 1)
				{
					List<LevelData> levelDatas = categoryInfos[levelData.CategoryIndex].LevelDatas;

					if (levelData.LevelIndex == levelDatas.Count - 1)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Invoked when the level complete popup closes
		/// </summary>
		private void OnLevelCompletePopupClosed(bool cancelled, object[] outData)
		{
			if (cancelled || outData == null || outData.Length != 1)
			{
				// Something went wrong, data is not how we expect it so just go back
				ScreenManager.Instance.Back();

				return;
			}

			// Get the players action from the popup
			string action = outData[0] as string;

			switch (action)
			{
				case LevelCompletePopup.PlayNextAction:
					PlayNextLevel(CurrentActiveLevel);
					break;
			}
		}

		/// <summary>
		/// Plays the next level
		/// </summary>
		private void PlayNextLevel(ActiveLevel level)
		{
			// Get the next pack, category, and level index to play
			PackInfo		packInfo;
			CategoryInfo	categoryInfo;
			int				levelIndex;

			if (!GetNextLevel(level, out packInfo, out categoryInfo, out levelIndex))
			{
				// If GetNextLevel returned false then it could not find the next level because the last level was just completed
				ScreenManager.Instance.Home();

				UIController.Instance.UpdateUI();

				return;
			}

			// Start the level
			StartLevel(packInfo, categoryInfo, levelIndex);
		}

		/// <summary>
		/// Gets the level that should be played after the given ActiveLevel
		/// </summary>
		private bool GetNextLevel(ActiveLevel level, out PackInfo packInfo, out CategoryInfo categoryInfo, out int levelIndex)
		{
			packInfo		= level.packInfo;
			categoryInfo	= level.categoryInfo;

			int packIndex		= packInfos.IndexOf(packInfo);
			int categoryIndex	= packInfo.categoryInfos.IndexOf(categoryInfo);

			// The next levels index is the given levels category level number since that number starts at 1
			levelIndex = level.levelData.CategoryLevelNumber;

			// Check if this is the las level in the category
			if (levelIndex >= categoryInfo.levelFiles.Count)
			{
				// Move to next category
				categoryIndex++;
				levelIndex = 0;

				// Check if this is the last category in the pack
				if (categoryIndex >= packInfo.categoryInfos.Count)
				{
					// Move to the next pack
					packIndex++;
					categoryIndex = 0;

					// Check if this is the last pack
					if (packIndex >= packInfos.Count)
					{
						// The given level is the last level in the game so return false to indicate we could not find a next level to play
						return false;
					}
				}
			}

			// Re-assign the pack and category info incase they changed
			packInfo		= packInfos[packIndex];
			categoryInfo	= packInfo.categoryInfos[categoryIndex];

			return true;
		}

		/// <summary>
		/// Called when a cell in the WordBoardGrid is clicked
		/// </summary>
		private void OnWordGridCellClicked(int row, int col)
		{
			if (CurrentActiveLevel == null || !PlayerSelectingHint)
			{
				return;
			}

			if (CurrentActiveLevel.levelData.LevelType == LevelData.Type.Grid)
			{
				// Check if there is a letter shown in the cell
				bool isLetterShown = (CurrentActiveLevel.levelSaveData.characterGrid[row][col] != ' ');

				// If the player is selecting a hint to show and there is no letter shown on this cell then show the letter
				if (!isLetterShown)
				{
					// Get any WordData that this cell belongs to
					int			letterIndex	= 0;
					WordData	wordData	= CurrentActiveLevel.levelData.GetLevelWordData(row, col, out letterIndex);

					// Show the letter on the grid
					ShowLetterForTargetHint(wordData, letterIndex);
				}
			}
			// Else it's a List type level
			else
			{
				// The row will be the index into the words list
				WordData wordData = CurrentActiveLevel.levelData.Words[row];

				// Check if the word has not been found yet and the hint for the letter has not been shown yet
				if (!CurrentActiveLevel.levelSaveData.foundWords.Contains(wordData.Word) &&
				    !CurrentActiveLevel.levelSaveData.hintIndicesUsed[wordData.Word][col])
				{
					ShowLetterForTargetHint(wordData, col);
				}
			}
		}

		/// <summary>
		/// Deducts the coins for using a player select hint and shows the letter on the given word/letterIndex
		/// </summary>
		private void ShowLetterForTargetHint(WordData wordData, int letterIndex)
		{
			// Deduct the cost for using a target hint now
			Coins -= CoinCostPerTargetHint;

			CoinController.Instance.SetCoinsText(Coins);

			EndPlayerSelectingHint();

			// Show the letter on the grid
			ShowLetterForHint(CurrentActiveLevel, wordData, letterIndex);

			SoundManager.Instance.Play("hint-used");
		}

		/// <summary>
		/// Loads all the words from the WordFile and places them in the allWords hashset
		/// </summary>
		private void LoadWordFile()
		{
			loadExtraWordsWorker = new LoadExtraWordsWorker();

			loadExtraWordsWorker.wordFileContents = wordFile.text;

			new System.Threading.Thread(new System.Threading.ThreadStart(loadExtraWordsWorker.Run)).Start();
		}

		/// <summary>
		/// Saves the current game state
		/// </summary>
		public Dictionary<string, object> Save()
		{
			// Get a list of all the level save data jsons
			List<object> levelJsons = new List<object>();

			foreach (KeyValuePair<string, LevelSaveData> pair in levelSaveDatas)
			{
				string			levelId			= pair.Key;
				LevelSaveData	levelSaveData	= pair.Value;

				Dictionary<string, object> levelJson = new Dictionary<string, object>();

				levelJson["id"]		= levelId;
				levelJson["data"]	= levelSaveData.Save();

				levelJsons.Add(levelJson);
			}

			// Create the main save data json object
			Dictionary<string, object> json = new Dictionary<string, object>();

			json["version"]						= SaveVersion;
			json["levels"]						= levelJsons;
			json["coins"]						= Coins;
			json["game_points"]					= GamePoints;
			json["num_levels_till_ad_shown"]	= NumLevelsTillAdShows;
			json["last_completed_level_number"]	= LastCompletedLevelNumber;

			return json;
		}

		/// <summary>
		/// Loads the saved game state
		/// </summary>
		private bool LoadSave()
		{
			levelSaveDatas = new Dictionary<string, LevelSaveData>();

			// Changed in 1.4 to use ISaveable instead, if SaveFilePath exists then we load it and delete it so the new save system is used next time
			if (System.IO.File.Exists(SaveFilePath))
			{
				JSONNode json = JSON.Parse(System.IO.File.ReadAllText(SaveFilePath));

				ParseSaveData(json);

				System.IO.File.Delete(SaveFilePath);

				return true;
			}
			else if (SaveManager.Exists())
			{
				JSONNode json = SaveManager.Instance.LoadSave(this);

				if (json == null)
				{
					return false;
				}

				ParseSaveData(json);

				return true;
			}

			return false;
		}

		private void ParseSaveData(JSONNode json)
		{
			GamePoints					= json["game_points"].AsInt;
			NumLevelsTillAdShows		= json["num_levels_till_ad_shown"].AsInt;
			Coins						= json["coins"].AsInt;
			LastCompletedLevelNumber	= json["last_completed_level_number"].AsInt;

			// Check if the save file verion has changed
			bool saveVersionChanged = (SaveVersion != json["version"].AsInt);
			HashSet<string> completedLevelIds = new HashSet<string>(); // Needed if updating from version 1 to 2

			// Load the level save data
			foreach (JSONNode levelJson in json["levels"].AsArray)
			{
				string			levelId			= levelJson["id"].Value;
				LevelSaveData	levelSaveData	= new LevelSaveData(levelJson["data"]);

				// If the save version has changed we need to check a few things.
				if (saveVersionChanged)
				{
					// Check if the levelJson contains the "completed" key and if it's true
					if (levelJson["data"]["completed"].AsBool && !completedLevelIds.Contains(levelId))
					{
						completedLevelIds.Add(levelId);
					}

					// Check if anything has changed in the level since starting it, if not then we do not need to add this level to the list of saved levels
					if (!levelSaveData.HasChanged())
					{
						continue;
					}
				}

				levelSaveDatas[levelId] = levelSaveData;
			}

			if (saveVersionChanged && completedLevelIds.Count > 0)
			{
				// Set the last completed level number to the last level that have the "completed" flag set to true
				LastCompletedLevelNumber = FindLastCompletedLevelNumber(completedLevelIds);
			}
		}



		/// <summary>
		/// Gets the level number for the given levelId
		/// </summary>
		private int FindLastCompletedLevelNumber(HashSet<string> completedLevelIds)
		{
			LevelData lastCompletedLevelData = null;

			for (int i = 0; i < packInfos.Count; i++)
			{
				PackInfo packInfo = PackInfos[i];

				for (int j = 0; j < packInfo.categoryInfos.Count; j++)
				{
					CategoryInfo categoryInfo = packInfo.categoryInfos[j];

					for (int k = 0; k < categoryInfo.LevelDatas.Count; k++)
					{
						LevelData levelData = categoryInfo.LevelDatas[k];

						if (completedLevelIds.Contains(levelData.Id))
						{
							lastCompletedLevelData = levelData;
						}
					}
				}
			}

			return (lastCompletedLevelData == null) ? 0 : lastCompletedLevelData.GameLevelNumber;
		}

		/// <summary>
		/// Searches all levels for the givne levelId and returns the pack, category, and level info for that level
		/// </summary>
		private bool FindInfos(string levelId, out PackInfo packInfo, out CategoryInfo categoryInfo, out LevelData levelData)
		{
			for (int i = 0; i < PackInfos.Count; i++)
			{
				packInfo = PackInfos[i];

				for (int j = 0; j < packInfo.categoryInfos.Count; j++)
				{
					categoryInfo = packInfo.categoryInfos[j];

					for (int k = 0; k < categoryInfo.LevelDatas.Count; k++)
					{
						levelData = categoryInfo.LevelDatas[k];

						if (levelId == levelData.Id)
						{
							return true;
						}
					}
				}
			}

			packInfo		= null;
			categoryInfo	= null;
			levelData		= null;

			return false;
		}

		#endregion
	}
}
