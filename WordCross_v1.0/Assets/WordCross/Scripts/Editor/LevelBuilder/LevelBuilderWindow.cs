using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WordCross
{
	public class LevelBuilderWindow : EditorWindow
	{
		#region Classes

		private class PreviewLetter
		{
			public char letter;
			public bool isNew;
			public bool isReused;
		}

		#endregion

		#region Enums

		private enum SortType
		{
			Rank,
			RankShortestFirst,
			RankLongestFirst,
			Alphabetically,
			AlphabeticallyShortestFirst,
			AlphabeticallyLongestFirst
		}

		#endregion

		#region Variables

		private const int WordsPerPage = 15;

		// Window Settings field variables
		private TextAsset	wordFile;
		private BoardType	boardType;
		private int			preferredBoardWidthRatio	= 1;
		private int			preferredBoardHeightRatio	= 1;
		private int			maxBoards					= 20000;
		private bool		batchMode = true;

		// Import field variables
		private TextAsset importLevelFile;

		// Level Settings field variables
		private int			letterCount = 3;
		private string		customWord;
		private SortType	sortType;
		private int			possibleWordsPageIndex;

		// Export field variables
		private Object		outputFolder;
		private string		filename;

		// Batch mode field variables
		private int				numberOfLevels;
		private int				minWordsPerLevel;
		private int				maxWordsPerLevel;
		private int				maxWordLength;
		private int				minWordLength;
		private LengthPickType	lengthPickType;
		private RankPickType	rankPickType;
		private bool			chooseBonusWord;
		private int				everyNumberOfLevels;
		private bool			includeFirstLevel;
		private int				maxBonusWordLength;
		private int				minBonusWordLength;
		private bool			reuseWords;
		private Object			loadWordsFromFolder;
		private int				maxReuseWordLength;
		private int				minReuseWordLength;
		private bool			overwriteExistingFiles;

		private Dictionary<string, List<RankedWord>>	wordDictionary;
		private Dictionary<int, List<RankedWord>>		wordsByLength;
		private List<string>							choosenWords;
		private List<bool>								choosenWordsStar;
		private string									choosenSortedLetters;	// All uniqe letters from the list of choosen words
		private HashSet<string>							filterWords;
		private List<PreviewLetter>						previewLetters;			// Sorted letters that contains both choosenSortedLetters and the selectedWord
		private List<RankedWord>						possibleWords;
		private GridBoardWorker.Board					board;
		private GridBoardWorker							gridBoardWorker;
		private BatchLevelCreationWorker				batchLevelCreationWorker;

		private Vector2			wordListScrollPosition;
		private Vector2			windowsScrollPosition;
		private Texture2D		lineTexture;

		#endregion

		#region Properties

		/// <summary>
		/// Gets and sets the path to the last word file so when the window closes/opens we can set the wordFile reference 
		/// </summary>
		private string WordFileAssetPath
		{
			get { return EditorPrefs.GetString("WordFileAssetPath", ""); }
			set { EditorPrefs.SetString("WordFileAssetPath", value); }
		}

		/// <summary>
		/// Gets and sets the path to the last output folder so when the window closes/opens we can set the outputFolder reference 
		/// </summary>
		private string OutputFilderAssetPath
		{
			get { return EditorPrefs.GetString("OutputFilderAssetPath", ""); }
			set { EditorPrefs.SetString("OutputFilderAssetPath", value); }
		}

		/// <summary>
		/// Gets and sets the path to the last output folder so when the window closes/opens we can set the outputFolder reference 
		/// </summary>
		private string LoadWordsFromFolderAssetPath
		{
			get { return EditorPrefs.GetString("LoadWordsFromFolderAssetPath", ""); }
			set { EditorPrefs.SetString("LoadWordsFromFolderAssetPath", value); }
		}

		// Getters for lists and dictionaries so they are never null
		private Dictionary<string, List<RankedWord>>	WordDictionary		{ get { return (wordDictionary == null) ? wordDictionary = new Dictionary<string, List<RankedWord>>() : wordDictionary; } }
		private Dictionary<int, List<RankedWord>>		WordsByLength		{ get { return (wordsByLength == null) ? wordsByLength = new Dictionary<int, List<RankedWord>>() : wordsByLength; } }
		private List<PreviewLetter>						PreviewLetters		{ get { return (previewLetters == null) ? previewLetters = new List<PreviewLetter>() : previewLetters; } }
		private List<string>							ChoosenWords		{ get { return (choosenWords == null) ? choosenWords = new List<string>() : choosenWords; } }
		private List<bool>								ChoosenWordsStar	{ get { return (choosenWordsStar == null) ? choosenWordsStar = new List<bool>() : choosenWordsStar; } }
		private List<RankedWord>						PossibleWords		{ get { return (possibleWords == null) ? possibleWords = new List<RankedWord>() : possibleWords; } set { possibleWords = value; } }
		private string									BonusWord			{ get; set; }

		/// <summary>
		/// Returns true if the GridBoardWorker is currently running
		/// </summary>
		private bool IsGridBoardWorkerRunning { get { return gridBoardWorker != null && !gridBoardWorker.Stopped; } }

		/// <summary>
		/// Gets the lineTexture, creates one if it is null
		/// </summary>
		private Texture2D LineTexture { get { return (lineTexture != null) ? lineTexture : (lineTexture = Utilities.CreateTexture(1, 1, GUI.skin.label.normal.textColor)); } }

		#endregion

		private void OnEnable()
		{
			if (wordFile == null && !string.IsNullOrEmpty(WordFileAssetPath))
			{
				// Load the word file from the saved path
				wordFile = AssetDatabase.LoadAssetAtPath<TextAsset>(WordFileAssetPath);

				// Check if we successfully got the word file reference
				if (wordFile == null)
				{
					Debug.LogError("[LevelBuilderWindow] Could not load previous set word file, it has either been deleted or moved.");

					WordFileAssetPath = "";
				}
			}

			if (outputFolder == null && !string.IsNullOrEmpty(OutputFilderAssetPath))
			{
				outputFolder = AssetDatabase.LoadAssetAtPath<Object>(OutputFilderAssetPath);
			}

			if (loadWordsFromFolder == null && !string.IsNullOrEmpty(LoadWordsFromFolderAssetPath))
			{
				loadWordsFromFolder = AssetDatabase.LoadAssetAtPath<Object>(LoadWordsFromFolderAssetPath);
			}

			if (wordFile != null)
			{
				// Need to re-process the word file since dictionaries are not serialzed
				ProcessWordFile();
			}

			if (string.IsNullOrEmpty(customWord))
			{
				customWord = "";
			}

			UpdateAll();
		}

		private void OnDisable()
		{
			DestroyTexture(lineTexture);
		}

		private void Update()
		{
			if (gridBoardWorker != null && gridBoardWorker.Stopped)
			{
				// Set the board to the grid board workers best board
				board = gridBoardWorker.BestBoard;

				Repaint();

				gridBoardWorker = null;
			}

			if (batchLevelCreationWorker != null)
			{
				if (!batchLevelCreationWorker.Stopped)
				{
					float	progress		= batchLevelCreationWorker.Progress;
					int		numberOfLevels	= batchLevelCreationWorker.NumberOfLevels;
					int		levelNumber		= Mathf.RoundToInt(progress * numberOfLevels);
					string	title			= "Batch Level Generator";
					string	message			= string.Format("Generating level {0} of {1}", levelNumber + 1, numberOfLevels);

					bool cancelled = EditorUtility.DisplayCancelableProgressBar(title, message, progress);

					if (cancelled)
					{
						Debug.Log("Stopping batch generation");

						batchLevelCreationWorker.Stop();
					}
				}
				else
				{
					EditorUtility.ClearProgressBar();

					if (!string.IsNullOrEmpty(batchLevelCreationWorker.Message))
					{
						Debug.LogError(batchLevelCreationWorker.Message);
					}
					else
					{
						Debug.Log("Batch generator done");
					}
					
					batchLevelCreationWorker = null;

					AssetDatabase.Refresh();
				}
			}
		}

		#region GUI Draw Methods

		private void OnGUI()
		{
			windowsScrollPosition = EditorGUILayout.BeginScrollView(windowsScrollPosition);

			EditorGUILayout.Space();

			DrawWindow();

			EditorGUILayout.Space();

			GUI.enabled = true;

			EditorGUILayout.EndScrollView();
		}

		private void DrawWindow()
		{
			DrawWindowSettings();

			EditorGUILayout.Space();

			if (batchMode)
			{
				DrawWindowForBatchMode();
			}
			else
			{
				DrawWindowForNormalMode();
			}

			EditorGUILayout.Space();

			DrawExportSettings();
		}

		private void DrawWindowSettings()
		{
			BeginBox("General");

			// Draw the word file field
			TextAsset newWordFile = EditorGUILayout.ObjectField("Word File", wordFile, typeof(TextAsset), false) as TextAsset;

			// Check if the word file changed
			if (wordFile != newWordFile)
			{
				wordFile = newWordFile;

				ResetWindow();

				if (wordFile != null)
				{
					WordFileAssetPath = AssetDatabase.GetAssetPath(wordFile);

					ProcessWordFile();
				}

				UpdateAll();
			}

			if (wordFile == null)
			{
				EditorGUILayout.HelpBox("Word File has not been assigned. Please drag a text file that contains all the possible words in the game.", MessageType.Warning);
			
				// Disable the rest of the GUI if there is no word file
				GUI.enabled = false;
			}

			// Draw the batch mode field
			batchMode = EditorGUILayout.Toggle("Batch Mode", batchMode);

			// Draw the board type field
			BoardType newBoardType = (BoardType)EditorGUILayout.EnumPopup("Board Type", boardType);

			if (boardType != newBoardType)
			{
				boardType = newBoardType;

				UpdateAll();
			}

			// Draw the preferred ratio field if the board type is grid
			if (boardType == BoardType.Grid)
			{
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.LabelField("Preferred Board Ratio:", GUILayout.Width(145));

				EditorGUILayout.LabelField("Width:", GUILayout.Width(45));
				preferredBoardWidthRatio = EditorGUILayout.IntField(preferredBoardWidthRatio);

				EditorGUILayout.LabelField("Height:", GUILayout.Width(45));
				preferredBoardHeightRatio = EditorGUILayout.IntField(preferredBoardHeightRatio);

				EditorGUILayout.EndHorizontal();

				maxBoards = Mathf.Max(1, EditorGUILayout.IntField("Max Boards To Generate:", maxBoards));
			}

			EndBox();
		}

		private void DrawWindowForNormalMode()
		{
			DrawImport();

			EditorGUILayout.Space();

			DrawLevelControls();

			float boxWidth = position.width - 10f;

			GUILayout.Space(-6f);

			EditorGUILayout.BeginHorizontal(GUILayout.Width(boxWidth));
			DrawLevelWords(boxWidth / 2f, 300f);
			GUILayout.Space(-2f);
			DrawPossibleWordList(boxWidth / 2f, 300f);
			EditorGUILayout.EndHorizontal();

			if (boardType == BoardType.Grid)
			{
				GUILayout.Space(-6f);
				
				DrawWordGrid();
			}
		}

		private void DrawImport()
		{
			BeginBox("Import");

			importLevelFile = EditorGUILayout.ObjectField("Level File", importLevelFile, typeof(TextAsset), false) as TextAsset;

			bool guiEnabled = GUI.enabled;

			GUI.enabled = guiEnabled && importLevelFile != null;

			if (GUILayout.Button("Import Level File"))
			{
				ImportLevelFile(importLevelFile);

				importLevelFile = null;
			}

			GUI.enabled = guiEnabled;

			EndBox();
		}

		private float DrawLevelControls()
		{
			// Get the previous value of GUI.enabled
			bool guiEnabled = GUI.enabled;

			Rect boxRect = BeginBox("Level");

			// Draw the letter count. Cannot be less than 2
			DrawLetterCount();

			// Get the word that we are considering to choose
			string newSelectedWord = EditorGUILayout.TextField("Custom Word: ", customWord);

			if (newSelectedWord != customWord)
			{
				customWord = newSelectedWord;

				// Updates the list of preview letters given the new selected word
				UpdatePreviewLetters();
			}

			if (!string.IsNullOrEmpty(customWord) && PreviewLetters.Count > letterCount)
			{
				EditorGUILayout.HelpBox("This word will cause the board to have more than the desired amount of letters.", MessageType.Warning);
			}

			// If there is no selected word then disable the Choose Words button
			if (string.IsNullOrEmpty(customWord))
			{
				GUI.enabled = false;
			}

			if (GUILayout.Button("Add Custom Word"))
			{
				ChooseWord(customWord);

				GUIUtility.keyboardControl = 0;
			}

			// Set GUI.enabled back to what it was before
			GUI.enabled = guiEnabled;

			if (GUILayout.Button("Clear All Words"))
			{
				ResetLevel();
			}

			EndBox();

			return boxRect.width;
		}

		private void DrawLetterCount()
		{
			// Draw the letter count. Cannot be less than 2
			int newLetterCount = Mathf.Max(2, EditorGUILayout.IntField("Letters In Level: ", letterCount));

			// If the amount of letters has changed then we need to update the word list to show the new list of possible words
			if (newLetterCount != letterCount)
			{
				letterCount = newLetterCount;

				UpdatePossibleWords();
			}
		}

		private void DrawLevelWords(float width, float height)
		{
			BeginBox("Letters In Level", width, height);

			int	lettersPerRow	= Mathf.FloorToInt(width / 25f);
			int	letterIndex		= 0;

			EditorGUILayout.Space();

			while (lettersPerRow > 0 && letterIndex < PreviewLetters.Count)
			{
				EditorGUILayout.BeginHorizontal();

				for (int i = 0; i < lettersPerRow && letterIndex < PreviewLetters.Count; i++, letterIndex++)
				{
					PreviewLetter previewLetter = PreviewLetters[letterIndex];

					EditorGUILayout.LabelField(previewLetter.letter.ToString(), GUILayout.Width(20));
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();

			DrawHeader("Words In Level");

			for (int i = 0; i < ChoosenWords.Count; i++)
			{
				string	choosenWord		= ChoosenWords[i];
				bool	isWordStarred	= ChoosenWordsStar[i];
				bool	isBonusWord		= (choosenWord == BonusWord);

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(choosenWord);

				if (boardType == BoardType.Grid)
				{
					bool setAsBonus = GUILayout.Toggle(isBonusWord, "Bonus", new GUIStyle(GUI.skin.button));

					if (setAsBonus && !isBonusWord)
					{
						BonusWord = choosenWord;

						UpdateBoard();
					}
				}

				ChoosenWordsStar[i] = GUILayout.Toggle(isWordStarred, "*", new GUIStyle(GUI.skin.button), GUILayout.Width(20f));

				if (GUILayout.Button("-", GUILayout.Width(20f)))
				{
					if (isBonusWord)
					{
						BonusWord = "";
					}

					ChoosenWords.RemoveAt(i);
					ChoosenWordsStar.RemoveAt(i);

					i--;

					UpdateAll();
				}

				EditorGUILayout.EndHorizontal();
			}

			EndBox();
		}

		private void DrawPossibleWordList(float width, float height)
		{
			BeginBox("Possible Words To Add", width, height);

			// Draw the sort type field
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Sort:", GUILayout.Width(50f));
			SortType newSortType = (SortType)EditorGUILayout.EnumPopup(sortType);

			EditorGUILayout.EndHorizontal();

			if (sortType != newSortType)
			{
				sortType = newSortType;

				SortPossibleWords();
			}

			EditorGUILayout.Space();

			bool guiEnabled = GUI.enabled;

			// Draw the prev/next buttons
			EditorGUILayout.BeginHorizontal();

			GUILayout.Space(3f);

			GUI.enabled = guiEnabled && possibleWordsPageIndex > 0;

			if (GUILayout.Button("< PREV", GUILayout.Height(18f)))
			{
				possibleWordsPageIndex -= WordsPerPage;

				GUIUtility.keyboardControl = 0;
			}

			GUI.enabled = guiEnabled;	

			int pageNumber = Mathf.FloorToInt((float)possibleWordsPageIndex / (float)WordsPerPage) + 1;
			int numPages = Mathf.CeilToInt((float)PossibleWords.Count / (float)WordsPerPage);

			EditorGUILayout.BeginVertical(GUILayout.Width(100f));
			GUILayout.Space(3f);
			EditorGUILayout.BeginHorizontal(GUILayout.Width(100f));
			GUILayout.Space(10f);
			int newPageNumber = EditorGUILayout.IntField(pageNumber, GUILayout.Width(35f));
			GUILayout.Space(-3f);
			EditorGUILayout.LabelField(" of " + numPages, GUILayout.Width(50f));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			if (pageNumber != newPageNumber)
			{
				pageNumber				= Mathf.Clamp(newPageNumber, 1, numPages);
				possibleWordsPageIndex	= (pageNumber - 1) * WordsPerPage;
			}

			GUI.enabled = guiEnabled && pageNumber < numPages;

			if (GUILayout.Button("NEXT >", GUILayout.Height(18f)))
			{
				possibleWordsPageIndex += WordsPerPage;

				GUIUtility.keyboardControl = 0;
			}

			GUI.enabled = guiEnabled;

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			// Draw the list of possible words
			wordListScrollPosition = EditorGUILayout.BeginScrollView(wordListScrollPosition);

			for (int i = 0; i < WordsPerPage && possibleWordsPageIndex + i < PossibleWords.Count; i++)
			{
				string word = PossibleWords[i + possibleWordsPageIndex].word.ToUpper();

				if (GUILayout.Button(word))
				{
					GUIUtility.keyboardControl = 0;

					ChooseWord(word);
				}
			}

			EditorGUILayout.EndScrollView();

			EndBox();
		}

		private void DrawWordGrid()
		{
			BeginBox("Word Grid");

			bool guiEnabled = GUI.enabled;

			GUI.enabled = guiEnabled && board != null;

			if (GUILayout.Button("Generate New Random Board"))
			{
				UpdateBoard();
			}

			GUI.enabled = guiEnabled;

			if (IsGridBoardWorkerRunning)
			{
				EditorGUILayout.LabelField("Generating new board...");
			}

			if (board != null)
			{
				EditorGUILayout.LabelField("Rows: " + board.numRows);
				EditorGUILayout.LabelField("Columns: " + board.numCols);

				for (int i = 0; i < board.letters.Length; i++)
				{
					EditorGUILayout.BeginHorizontal();

					for (int j = 0; j < board.letters[i].Length; j++)
					{
						char letter = board.letters[i][j];

						EditorGUILayout.LabelField(letter == (char)0 ? "" : letter.ToString(), GUILayout.Width(20));
					}

					EditorGUILayout.EndHorizontal();
				}
			}

			EndBox();
		}

		private void DrawWindowForBatchMode()
		{
			BeginBox("Batch Level Creation");

			DrawLetterCount();

			EditorGUILayout.Space();

			numberOfLevels		= Mathf.Max(EditorGUILayout.IntField("Number Of Levels", numberOfLevels), 1);
			minWordsPerLevel	= Mathf.Max(EditorGUILayout.IntField("Min Words Per Level", minWordsPerLevel), 0);
			maxWordsPerLevel	= Mathf.Max(EditorGUILayout.IntField("Max Words Per Level", maxWordsPerLevel), 0);
			minWordLength		= Mathf.Clamp(EditorGUILayout.IntField("Min Word Length", minWordLength), 0, letterCount);
			maxWordLength		= Mathf.Clamp(EditorGUILayout.IntField("Max Word Length", maxWordLength), 0, letterCount);
			lengthPickType		= (LengthPickType)EditorGUILayout.EnumPopup("Length Pick Type", lengthPickType);
			rankPickType		= (RankPickType)EditorGUILayout.EnumPopup("Rank Pick Type", rankPickType);

			EditorGUILayout.Space();

			bool guiEnabled = GUI.enabled;

			chooseBonusWord = EditorGUILayout.Toggle("Choose Bonus Word", chooseBonusWord);

			GUI.enabled = guiEnabled && chooseBonusWord;

			everyNumberOfLevels	= Mathf.Max(EditorGUILayout.IntField("Every # of Levels", everyNumberOfLevels), 1);
			includeFirstLevel	= EditorGUILayout.Toggle("Include First Level", includeFirstLevel);
			minBonusWordLength	= Mathf.Clamp(EditorGUILayout.IntField("Min Bonus Word Length", minBonusWordLength), 0, letterCount);
			maxBonusWordLength	= Mathf.Clamp(EditorGUILayout.IntField("Max Bonus Word Length", maxBonusWordLength), 0, letterCount);

			EditorGUILayout.Space();

			GUI.enabled = guiEnabled;

			reuseWords			= EditorGUILayout.Toggle("Re-Use Words", reuseWords);
			loadWordsFromFolder	= EditorGUILayout.ObjectField("Load Words From Folder", loadWordsFromFolder, typeof(Object), false);
			
			GUI.enabled = guiEnabled && reuseWords;
			
			minReuseWordLength	= Mathf.Clamp(EditorGUILayout.IntField("Min Re-Use Word Length", minReuseWordLength), 0, letterCount);
			maxReuseWordLength	= Mathf.Clamp(EditorGUILayout.IntField("Max Re-Use Word Length", maxReuseWordLength), 0, letterCount);

			GUI.enabled = guiEnabled;

			EditorGUILayout.Space();

			EndBox();
		}

		private void DrawExportSettings()
		{
			BeginBox("Export");

			outputFolder = EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(Object), false);

			OutputFilderAssetPath = (outputFolder != null) ? AssetDatabase.GetAssetPath(outputFolder) : null;

			string folderPath = LevelBuilderUtilities.GetOutputFolderPath(outputFolder);

			// Get the file name / file name prefix to use
			if (batchMode)
			{
				filename				= EditorGUILayout.TextField("Filename Prefix", filename);
				overwriteExistingFiles	= EditorGUILayout.Toggle("Overwrite Existing Files", overwriteExistingFiles);
			}
			else
			{
				filename = EditorGUILayout.TextField("Filename", filename);
			}

			string folderAssetPath = "Assets" + folderPath.Substring(Application.dataPath.Length);

			// Draw the output path
			if (batchMode)
			{
				EditorGUILayout.LabelField("Files will be saved to: " + folderAssetPath + "/" + filename + "<#>.json");
			}
			else
			{
				string fullFileName = (string.IsNullOrEmpty(filename) ? "<id>" : filename) + ".json";

				EditorGUILayout.LabelField("File will be saved to: " + folderAssetPath + "/" + fullFileName);
			}

			if (batchMode && GUILayout.Button("Generate All Levels"))
			{
				StartBatchGeneration();
			}

			if (!batchMode)
			{
				bool guiEnabled = GUI.enabled;

				// Disable the export button while a board is being created
				GUI.enabled = guiEnabled && !IsGridBoardWorkerRunning;

				if (GUILayout.Button("Export Level"))
				{
					ExportLevel(folderPath);
				}

				GUI.enabled = guiEnabled;
			}

			EndBox();
		}

		private Rect BeginBox(string headerText = "", float width = 0, float height = 0)
		{
			Rect boxRect;

			if (width == 0f && height == 0f)
			{
				boxRect = EditorGUILayout.BeginVertical(GUI.skin.box);
			}
			else if (height == 0f)
			{
				boxRect = EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width));
			}
			else if (width == 0f)
			{
				boxRect = EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(width));
			}
			else
			{
				boxRect = EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width), GUILayout.Height(width));
			}

			if (!string.IsNullOrEmpty(headerText))
			{
				DrawHeader(headerText);
			}

			return boxRect;
		}

		private void EndBox()
		{
			GUILayout.Space(3f);
			EditorGUILayout.EndVertical();
		}

		private void DrawHeader(string headerText)
		{
			EditorGUILayout.LabelField(headerText);

			DrawLine();
		}

		private void DrawLine()
		{
			Rect rect = GUILayoutUtility.GetLastRect();

			rect.y += rect.height;
			rect.height = 1;

			GUI.DrawTexture(rect, LineTexture);

			GUILayout.Space(4f);
		}

		#endregion // GUI Draw Methods

		/// <summary>
		/// Processes the wordFile. Places all words in a dictionary where the key is the words sorted alphabetically.
		/// </summary>
		private void ProcessWordFile()
		{
			if (wordFile == null)
			{
				Debug.LogError("[LevelBuilderWindow] Could not process word file: wordFile is null");
				return;
			}

			string[] words = wordFile.text.Split('\n');

			WordDictionary.Clear();
			WordsByLength.Clear();

			int count = 0;

			for (int i = 0; i < words.Length; i++)
			{
				string wordStr = words[i].Replace("\r", "").Trim().ToUpper();

				// Skip words that are less than 2 characters
				if (wordStr.Length < 2)
				{
					continue;
				}

				// Add the sorted word to the dictionary
				string		sortedWord	= LevelBuilderUtilities.SortAlphabetically(wordStr);
				RankedWord	word		= new RankedWord(wordStr, i);

				if (!WordDictionary.ContainsKey(sortedWord))
				{
					WordDictionary.Add(sortedWord, new List<RankedWord>());
				}

				WordDictionary[sortedWord].Add(word);

				// Add the word the the length dictionary
				if (!WordsByLength.ContainsKey(wordStr.Length))
				{
					WordsByLength.Add(wordStr.Length, new List<RankedWord>());
				}

				WordsByLength[wordStr.Length].Add(word);

				count++;
			}
		}

		/// <summary>
		/// Addes the given word to the list of choosen words and calls all update methods
		/// </summary>
		private void ChooseWord(string word)
		{
			if (ChoosenWords.Contains(word))
			{
				return;
			}

			ChoosenWords.Add(word);
			ChoosenWordsStar.Add(false);

			customWord = "";

			UpdateAll();
		}

		/// <summary>
		/// Updates choosenSortedLetters based on the list of ChoosenWords
		/// </summary>
		private void UpdateChoosenLetters()
		{
			choosenSortedLetters = LevelBuilderUtilities.GetLetters(ChoosenWords);
		}

		/// <summary>
		/// Updates the list of PreviewLetters based on the choosenSortedLetters and selectedWord
		/// </summary>
		private void UpdatePreviewLetters()
		{
			PreviewLetters.Clear();

			Dictionary<char, int> letterDictionary = new Dictionary<char, int>();

			for (int i = 0; i < choosenSortedLetters.Length; i++)
			{
				char letter = choosenSortedLetters[i];

				if (!letterDictionary.ContainsKey(letter))
				{
					letterDictionary.Add(letter, 0);
				}

				letterDictionary[letter]++;
			}

			// All all the letters from the new word indicating if they are a new letter or reusing a letter from the already choosen words
			for (int i = 0; i < customWord.Length; i++)
			{
				char letter	= customWord[i];
				bool inDic	= letterDictionary.ContainsKey(letter);

				if (inDic)
				{
					letterDictionary[letter]--;

					if (letterDictionary[letter] == 0)
					{
						letterDictionary.Remove(letter);
					}
				}

				PreviewLetter previewLetter = new PreviewLetter();

				previewLetter.letter	= letter;
				previewLetter.isNew		= !inDic;
				previewLetter.isReused	= inDic;

				PreviewLetters.Add(previewLetter);
			}

			// Add all the already choosen letters if they are still in the dictionary
			for (int i = 0; i < choosenSortedLetters.Length; i++)
			{
				char letter	= choosenSortedLetters[i];
				bool inDic	= letterDictionary.ContainsKey(letter);

				if (inDic)
				{
					letterDictionary[letter]--;

					if (letterDictionary[letter] == 0)
					{
						letterDictionary.Remove(letter);
					}

					PreviewLetter previewLetter = new PreviewLetter();

					previewLetter.letter = letter;

					PreviewLetters.Add(previewLetter);
				}
			}

			// Sort the preview letters
			PreviewLetters.Sort((PreviewLetter x, PreviewLetter y) => 
				{
					return (int)x.letter - (int)y.letter;
				});
		}

		private void UpdateAll()
		{
			UpdateChoosenLetters();
			UpdatePreviewLetters();
			UpdatePossibleWords();
			UpdateBoard();
		}

		private void ResetLevel()
		{
			ClearLevel();

			UpdatePreviewLetters();
			UpdatePossibleWords();
			UpdateBoard();
		}

		private void ClearLevel()
		{
			ChoosenWords.Clear();
			ChoosenWordsStar.Clear();

			customWord				= "";
			choosenSortedLetters	= "";
			BonusWord				= "";
			board					= null;
		}

		/// <summary>
		/// Clears all variables
		/// </summary>
		private void ResetWindow()
		{
			customWord				= "";
			choosenSortedLetters	= "";
			BonusWord				= "";
			board					= null;

			WordDictionary.Clear();
			WordsByLength.Clear();
			ChoosenWords.Clear();
			ChoosenWordsStar.Clear();
			PreviewLetters.Clear();
			PossibleWords.Clear();
		}

		/// <summary>
		/// Updates the list of PossibleWords based on choosenSortedLetters 
		/// </summary>
		private void UpdatePossibleWords()
		{
			// If choosenSortedLetters is empty then just get the list of words that have length equal to letterCount
			if (string.IsNullOrEmpty(choosenSortedLetters))
			{
				PossibleWords.Clear();

				for (int len = letterCount; len >= 2; len--)
				{
					if (WordsByLength.ContainsKey(len))
					{
						PossibleWords.AddRange(WordsByLength[len]);
					}
				}
			}
			// Else get all possible words for the level given the choosen words and the letterCount
			else
			{
				PossibleWords = LevelBuilderUtilities.GetWordsThatFit(choosenSortedLetters, letterCount, WordDictionary, WordsByLength);
			}

			// Remove already choosen words from the list of possible words
			for (int i = 0; i < PossibleWords.Count; i++)
			{
				if (ChoosenWords.Contains(PossibleWords[i].word))
				{
					PossibleWords.RemoveAt(i);
					i--;
				}
			}

			// Finally, sort the possible words
			SortPossibleWords();
		}

		/// <summary>
		/// Updates the preview for the board
		/// </summary>
		private void UpdateBoard()
		{
			//if (boardType == BoardType.List)
			//{
			//	return;
			//}

			// If there is already a worker then stop the thread
			if (gridBoardWorker != null)
			{
				gridBoardWorker.Stop();
			}

			// Set the current board to null since it's going to change
			board = null;

			if (ChoosenWords.Count > 0)
			{
				// Create a new worker
				gridBoardWorker					= new GridBoardWorker();
				gridBoardWorker.BonusWord		= BonusWord;
				gridBoardWorker.Words			= new List<string>(ChoosenWords);
				gridBoardWorker.PreferredRatio	= (preferredBoardHeightRatio == 0 || preferredBoardWidthRatio == 0) ? 0 : (float)preferredBoardHeightRatio / (float)preferredBoardWidthRatio;
				gridBoardWorker.MaxBoards		= maxBoards;

				if (!string.IsNullOrEmpty(BonusWord))
				{
					gridBoardWorker.Words.Remove(BonusWord);
				}

				// Run the worker on a new thread
				new System.Threading.Thread(new System.Threading.ThreadStart(gridBoardWorker.Run)).Start();
			}
		}

		private void StartBatchGeneration()
		{
			batchLevelCreationWorker = new BatchLevelCreationWorker();

			batchLevelCreationWorker.WordDictionary			= WordDictionary;
			batchLevelCreationWorker.WordsByLength			= WordsByLength;
			batchLevelCreationWorker.LetterCount			= letterCount;
			batchLevelCreationWorker.MinWords				= minWordsPerLevel;
			batchLevelCreationWorker.MaxWords				= maxWordsPerLevel;
			batchLevelCreationWorker.NumberOfLevels			= numberOfLevels;
			batchLevelCreationWorker.MaxWordLength			= maxWordLength;
			batchLevelCreationWorker.MinWordLength			= minWordLength;
			batchLevelCreationWorker.WordLengthPickType		= lengthPickType;
			batchLevelCreationWorker.WordRankPickType		= rankPickType;
			batchLevelCreationWorker.ChooseBonusWord		= chooseBonusWord;
			batchLevelCreationWorker.EveryNumberOfLevels	= everyNumberOfLevels;
			batchLevelCreationWorker.IncludeFirstLevel		= includeFirstLevel;
			batchLevelCreationWorker.MaxBonusWordLength		= maxBonusWordLength;
			batchLevelCreationWorker.MinBonusWordLength		= minBonusWordLength;
			batchLevelCreationWorker.ReuseWords				= reuseWords;
			batchLevelCreationWorker.LoadFromFolderPath		= LevelBuilderUtilities.GetFolderPath(loadWordsFromFolder);
			batchLevelCreationWorker.MaxReuseWordLength		= maxReuseWordLength;
			batchLevelCreationWorker.MinReuseWordLength		= minReuseWordLength;
			batchLevelCreationWorker.CreateBoard			= (boardType == BoardType.Grid);
			batchLevelCreationWorker.PreferredBoardRatio	= (preferredBoardHeightRatio == 0 || preferredBoardWidthRatio == 0) ? 0 : (float)preferredBoardHeightRatio / (float)preferredBoardWidthRatio;
			batchLevelCreationWorker.MaxBoards				= maxBoards;
			batchLevelCreationWorker.OutputFolderPath		= LevelBuilderUtilities.GetOutputFolderPath(outputFolder);
			batchLevelCreationWorker.FilenamePrefix			= filename;
			batchLevelCreationWorker.OverwriteExisting		= overwriteExistingFiles;

			// Run the worker on a new thread
			new System.Threading.Thread(new System.Threading.ThreadStart(batchLevelCreationWorker.Run)).Start();
		}

		private void ExportLevel(string folderPath)
		{
			if (ChoosenWords.Count == 0)
			{
				EditorUtility.DisplayDialog("No Words", "You have not added any words to the level. Please add words first before exporting the level.", "OK");

				return;
			}

			bool fileCreated = LevelBuilderUtilities.ExportLevelFile(choosenSortedLetters, boardType, board, ChoosenWords, ChoosenWordsStar, false, 0, folderPath, filename, false);

			if (!fileCreated)
			{
				if (EditorUtility.DisplayDialog("File Already Exists", "The level file already exists in the given folder, would you like to overwrite it?", "Yes", "No"))
				{
					// Call it again but with overwrite set to true
					fileCreated = LevelBuilderUtilities.ExportLevelFile(choosenSortedLetters, boardType, board, ChoosenWords, ChoosenWordsStar, false, 0, folderPath, filename, true);
				
					AssetDatabase.Refresh();

					return;
				}
			}

			if (fileCreated)
			{
				AssetDatabase.Refresh();

				EditorUtility.DisplayDialog("File Created", "Level file successfully created", "OK");
			}
		}

		private void ImportLevelFile(TextAsset levelFile)
		{
			string					letters;
			List<string>			wordsInLevel;
			List<bool>				wordIsStarred;
			BoardType				newBoardType;
			GridBoardWorker.Board	newBoard;

			bool loaded = LevelBuilderUtilities.LoadLevelFile(levelFile.text, out letters, out wordsInLevel, out wordIsStarred, out newBoardType, out newBoard);

			if (loaded)
			{
				ClearLevel();

				boardType	= newBoardType;
				board		= newBoard;

				letterCount = letters.Length;

				ChoosenWords.AddRange(wordsInLevel);
				ChoosenWordsStar.AddRange(wordIsStarred);

				UpdateChoosenLetters();
				UpdatePreviewLetters();
				UpdatePossibleWords();

				string levelFilePath	= AssetDatabase.GetAssetPath(importLevelFile);
				string folderPath		= System.IO.Path.GetDirectoryName(levelFilePath);
				
				filename = importLevelFile.name;
				outputFolder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
			}
		}

		#region Other Methods

		private void SortPossibleWords()
		{
			switch (sortType)
			{
				case SortType.Rank:
					PossibleWords.Sort((RankedWord word1, RankedWord word2) =>
					{
						return word1.rank - word2.rank;
					});
					break;
				case SortType.RankShortestFirst:
					PossibleWords.Sort((RankedWord word1, RankedWord word2) =>
					{
						int lengthDiff = word1.word.Length - word2.word.Length;

						if (lengthDiff != 0)
						{
							return lengthDiff;
						}

						return word1.rank - word2.rank;
					});
					break;
				case SortType.RankLongestFirst:
					PossibleWords.Sort((RankedWord word1, RankedWord word2) =>
					{
						int lengthDiff = word2.word.Length - word1.word.Length;

						if (lengthDiff != 0)
						{
							return lengthDiff;
						}

						return word1.rank - word2.rank;
					});
					break;
				case SortType.Alphabetically:
					PossibleWords.Sort((RankedWord word1, RankedWord word2) =>
					{
						return word1.word.CompareTo(word2.word);
					});
					break;
				case SortType.AlphabeticallyShortestFirst:
					PossibleWords.Sort((RankedWord word1, RankedWord word2) =>
					{
						int lengthDiff = word1.word.Length - word2.word.Length;

						if (lengthDiff != 0)
						{
							return lengthDiff;
						}

						return word1.word.CompareTo(word2.word);
					});
					break;
				case SortType.AlphabeticallyLongestFirst:
					PossibleWords.Sort((RankedWord word1, RankedWord word2) =>
					{
						int lengthDiff = word2.word.Length - word1.word.Length;

						if (lengthDiff != 0)
						{
							return lengthDiff;
						}

						return word1.word.CompareTo(word2.word);
					});
					break;
			}

			possibleWordsPageIndex = 0;
		}

		/// <summary>
		/// Destroys the given texture if it's not null
		/// </summary>
		private void DestroyTexture(Texture2D texture)
		{
			if (texture != null)
			{
				DestroyImmediate(texture);
			}
		}

		#endregion // Other Methods 
	}
}
