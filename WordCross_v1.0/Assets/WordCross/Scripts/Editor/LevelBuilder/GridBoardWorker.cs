using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WordCross
{
	public class GridBoardWorker : Worker
	{
		#region Classes

		public class Board
		{
			public int				numRows;
			public int				numCols;
			public List<BoardWord>	boardWords;
			public char[][]			letters;	// Only used when displaying the board in the LevelBuilderWindow
		}

		public class BoardWord
		{
			public string	word;
			public bool		isVertical;
			public int		rowIndex;
			public int		colIndex;
		}

		private class Grid
		{
			public List<List<GridCell>>	gridCells;
			public int					numRows;
			public int					numCols;
			public int					rowOrigin;
			public int					colOrigin;
			public List<GridCell>		placedLetters;
			public int					numPlacedWords;
			public bool					hasBonusWord;
			public GridWord				bonusWord;
		}

		private class GridCell
		{
			public int	rowOriginOffset;
			public int	colOriginOffset;
			public bool	canPlaceLetter;
			public char	letter;

			public bool HasLetter { get { return letter != (char)0; } }
		}

		private class CrossLetter
		{
			public char letter = '\0'; 
			public int	offset = 0;
		}

		private class GridWord
		{
			public string				word;
			public List<GridWordLetter>	gridWordLetters;
			public bool					isPlaced;
			public bool					isVertical;
			public int					rowOriginOffset;
			public int					colOriginOffset;
		}

		private class GridWordLetter
		{
			public GridWord gridWord;
			public char		letter;
			public int		index;
		}

		#endregion

		#region Enums

		private enum Direction
		{
			Vertical,
			Horizontal
		}

		#endregion

		#region Member Variables

		/// <summary>
		/// The current maximum number of boards to generate, this is changed as the algorithm runs so we get a good range of all possible random boards
		/// </summary>
		private int currentMaxBoards;

		/// <summary>
		/// Since this worker will be run in a seperate thread we cannt use UnityEngine.Random
		/// </summary>
		private System.Random random;

		#endregion

		#region Properties

		// In Values
		public string		BonusWord		{ get; set; }
		public List<string>	Words			{ get; set; }
		public float		PreferredRatio	{ get; set; }
		public int			MaxBoards		{ get; set; }

		// Out Values
		public Board BestBoard { get; set; }

		#endregion

		#region Public Methods

		protected override void Begin()
		{
			random = new System.Random();
		}

		protected override void DoWork()
		{
			Grid									grid		= CreateGrid(BonusWord);
			List<GridWord>							gridWords	= CreateGridWords(Words);
			Dictionary<char, List<GridWordLetter>>	letterMap	= CreateLetterMap(gridWords);

			// Get the list of starting GridCells
			List<GridCell> startingGridCells = GetStartingGridCells(grid);

			// Create an empty list of grid boards, we will fill this with all possible boards
			List<Board> gridBoards = new List<Board>();

			if (startingGridCells.Count == 1)
			{
				BeginCreateBoards(grid, gridWords, letterMap, gridBoards, gridWords[0], 0, 0, MaxBoards);
			}
			else
			{
				int maxBoardsPer	= Mathf.FloorToInt((float)MaxBoards / (float)(startingGridCells.Count * gridWords.Count));
				int extraBoards		= MaxBoards - maxBoardsPer * startingGridCells.Count * gridWords.Count;

				// For bonus words we need to try every word on every starting cell to get all possible boards
				for (int i = 0; i < startingGridCells.Count; i++)
				{
					GridCell startingGridCell = startingGridCells[i];

					for (int j = 0; j < gridWords.Count; j++)
					{
						GridWord gridWord = gridWords[j];

						BeginCreateBoards(grid, gridWords, letterMap, gridBoards, gridWord, startingGridCell.rowOriginOffset, startingGridCell.colOriginOffset, maxBoardsPer + extraBoards);

						extraBoards = 0;
					}
				}
			}

			if (!Stopping)
			{
				//Debug.LogFormat("Created {0} Boards", gridBoards.Count);

				// Find the board that best matches our criteria
				BestBoard = GetBestBoard(gridBoards);

				if (BestBoard != null)
				{
					// Set the letters array on this board so it can be displayed in the window
					SetBoardLettersArray(BestBoard);
				}

				//PrintBoardInfo(BestBoard);
				//PrintBoard(BestBoard);

				Stop();
			}
		}

		/// <summary>
		/// Sets the letters char array for the given board
		/// </summary>
		public static void SetBoardLettersArray(Board board)
		{
			// Initalize the board
			char[][] letters = new char[board.numRows][];

			for (int i = 0; i < board.numRows; i++)
			{
				letters[i] = new char[board.numCols];

				for (int j = 0; j < board.numCols; j++)
				{
					letters[i][j] = (char)0;
				}
			}

			// Set the letters for all the words
			for (int i = 0; i < board.boardWords.Count; i++)
			{
				BoardWord boardWord = board.boardWords[i];

				for (int j = 0; j < boardWord.word.Length; j++)
				{
					int rowIndex = boardWord.rowIndex + (boardWord.isVertical ? j : 0);
					int colIndex = boardWord.colIndex + (boardWord.isVertical ? 0 : j);

					letters[rowIndex][colIndex] = boardWord.word[j];
				}
			}

			// Assign the letters array to the board
			board.letters = letters;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates the initial list of GridWords that ust be placed on the board
		/// </summary>
		private List<GridWord> CreateGridWords(List<string> words)
		{
			List<GridWord> gridWords = new List<GridWord>();

			// Create a GridWord for all the words
			for (int i = 0; i < words.Count; i++)
			{
				gridWords.Add(CreateGridWord(words[i]));
			}

			return gridWords;
		}

		/// <summary>
		/// Creates a letter mapping of characters to words with those characters
		/// </summary>
		private Dictionary<char, List<GridWordLetter>> CreateLetterMap(List<GridWord> gridWords)
		{
			Dictionary<char, List<GridWordLetter>> letterMap = new Dictionary<char, List<GridWordLetter>>();

			for (int i = 0; i < gridWords.Count; i++)
			{
				GridWord gridWord = gridWords[i];

				for (int j = 0; j < gridWord.gridWordLetters.Count; j++)
				{
					GridWordLetter	gridWordLetter	= gridWord.gridWordLetters[j];
					char			letter			= gridWordLetter.letter;

					if (!letterMap.ContainsKey(letter))
					{
						// Add a new letter list to the map
						letterMap.Add(letter, new List<GridWordLetter>());
					}

					// Add the grid word letter to the map
					letterMap[letter].Add(gridWordLetter);
				}
			}

			return letterMap;
		}

		/// <summary>
		/// Create the inital grid
		/// </summary>
		private Grid CreateGrid(string bonusWord)
		{
			Grid grid = new Grid
			{
				// Initialize the grid with a single row/column/GridCell
				colOrigin		= 0,
				rowOrigin		= 0,
				numCols			= 1,
				numRows			= 1,
				gridCells		= new List<List<GridCell>>() { new List<GridCell>() { CreateGridCell(0, 0) } },
				placedLetters	= new List<GridCell>()
			};

			if (!string.IsNullOrEmpty(bonusWord))
			{
				GridWord bonusGridWord = CreateGridWord(bonusWord);

				// Place the bonus word on the grid
				PlaceWord(grid, bonusGridWord, 0, 0, Direction.Horizontal);

				// Clear the list of placed letters because we do not want other words to start on the bonus word
				grid.placedLetters.Clear();

				// Add two empty rows and two empty columns
				AddRow(grid, false);
				AddRow(grid, false);
				AddCol(grid, false);
				AddCol(grid, false);

				// Set the canPlaceLetter flag on all GridCells adjacent to the bouns word to false
				for (int i = 0; i <= bonusWord.Length; i++)
				{
					grid.gridCells[0][i].canPlaceLetter = false;
					grid.gridCells[1][i].canPlaceLetter = false;
				}

				grid.hasBonusWord	= true;
				grid.bonusWord		= bonusGridWord;

				// We now have a grid that looks like this:
				//
				// WWWWW-#
				// ------#
				// #######
				//
				// Where 'W' is the bouns words letters,
				//       '-' is the cells where no letters can be placed and
				//       '#' is the starting cells where any letters can be placed
			}

			return grid;
		}

		/// <summary>
		/// Creates a GridWord from the given string word
		/// </summary>
		private GridWord CreateGridWord(string word)
		{
			GridWord gridWord = new GridWord
			{
				word		= word,
				isPlaced	= false
			};

			gridWord.gridWordLetters = CreateGridWordLetters(gridWord);

			return gridWord;
		}

		/// <summary>
		/// Create a list of GridWordLetters for the given GridWord
		/// </summary>
		private List<GridWordLetter> CreateGridWordLetters(GridWord gridWord)
		{
			List<GridWordLetter> gridWordLetters = new List<GridWordLetter>();

			for (int i = 0; i < gridWord.word.Length; i++)
			{
				GridWordLetter gridWordLetter = CreateGridWordLetter(gridWord, i);

				gridWordLetters.Add(gridWordLetter);
			}

			return gridWordLetters;
		}

		/// <summary>
		/// Create a GridWordLetter for the given index in the given GridWord
		/// </summary>
		private GridWordLetter CreateGridWordLetter(GridWord gridWord, int index)
		{
			GridWordLetter gridWordLetter = new GridWordLetter
			{
				gridWord	= gridWord,
				letter		= gridWord.word[index],
				index		= index
			};

			return gridWordLetter;
		}

		/// <summary>
		/// Creates a GridCell object
		/// </summary>
		private GridCell CreateGridCell(int rowOriginOffset, int colOriginOffset, bool canPlaceLetter = true, char letter = (char)0)
		{
			GridCell gridCell = new GridCell
			{
				rowOriginOffset	= rowOriginOffset,
				colOriginOffset	= colOriginOffset,
				canPlaceLetter	= canPlaceLetter,
				letter			= letter
			};

			return gridCell;
		}

		/// <summary>
		/// Gets the list of starting GridCells where the first words can be placed
		/// </summary>
		private List<GridCell> GetStartingGridCells(Grid grid)
		{
			List<GridCell> startingGridCells = new List<GridCell>();

			for (int i = 0; i < grid.gridCells.Count; i++)
			{
				for (int j = 0; j < grid.gridCells[i].Count; j++)
				{
					GridCell gridCell = grid.gridCells[i][j];

					// Get all the GridCell which can have a letter placed on them
					if (gridCell.canPlaceLetter)
					{
						startingGridCells.Add(gridCell);
					}
				}
			}

			return startingGridCells;
		}

		/// <summary>
		/// Places the starting word on the board at the given row/col index then starts creating all possible boards
		/// </summary>
		private void BeginCreateBoards(Grid										grid,
		                               List<GridWord>							gridWords,
		                               Dictionary<char, List<GridWordLetter>>	letterMap,
		                               List<Board>								gridBoards,
		                               GridWord									startingGridWord,
		                               int										rowIndex,
		                               int										colIndex,
		                               int										maxBoardsToCreate)
		{
			currentMaxBoards += Mathf.CeilToInt((float)maxBoardsToCreate / 2f);

			PlaceWordAndCreateBoards(grid, gridWords, letterMap, gridBoards, startingGridWord, rowIndex, colIndex, Direction.Vertical);

			currentMaxBoards += Mathf.FloorToInt((float)maxBoardsToCreate / 2f);

			PlaceWordAndCreateBoards(grid, gridWords, letterMap, gridBoards, startingGridWord, rowIndex, colIndex, Direction.Horizontal);
		}

		/// <summary>
		/// Places the given GridWord on the grid and the given row/col index then calls CreateBoards to create all possible boards
		/// </summary>
		private void PlaceWordAndCreateBoards(Grid									grid,
		                                     List<GridWord>							gridWords,
		                                     Dictionary<char, List<GridWordLetter>>	letterMap,
		                                     List<Board>							gridBoards,
		                                     GridWord								gridWord,
		                                     int									rowIndex,
		                                     int									colIndex,
		                                     Direction								direction)
		{
			if (Stopping || gridBoards.Count >= currentMaxBoards)
			{
				return;
			}

			// Place the word on the grid
			int numPlacedLetters = PlaceWord(grid, gridWord, rowIndex, colIndex, direction);

			// Set the word as placed
			gridWord.isPlaced = true;
			grid.numPlacedWords++;

			// Create all the possible boards with the current grid
			CreateBoards(grid, gridWords, letterMap, gridBoards);

			// Set the word as no longer placed
			gridWord.isPlaced = false;
			grid.numPlacedWords--;

			// Remove the placed letters from the grid
			ClearPlacedLetters(grid, numPlacedLetters);
		}

		/// <summary>
		/// Places a starting word on the board
		/// </summary>
		private int PlaceWord(Grid grid, GridWord gridWord, int rowIndex, int colIndex, Direction direction)
		{
			int numLetterPlaced = 0;

			if (direction == Direction.Horizontal)
			{
				for (int i = 0; i < gridWord.gridWordLetters.Count; i++)
				{
					if (PlaceLetter(grid, gridWord.gridWordLetters[i].letter, rowIndex, colIndex + i))
					{
						numLetterPlaced++;
					}
				}
			}
			else
			{
				for (int i = 0; i < gridWord.gridWordLetters.Count; i++)
				{
					if (PlaceLetter(grid, gridWord.gridWordLetters[i].letter, rowIndex + i, colIndex))
					{
						numLetterPlaced++;
					}
				}
			}

			gridWord.isVertical			= (direction == Direction.Vertical);
			gridWord.rowOriginOffset	= rowIndex - grid.rowOrigin;
			gridWord.colOriginOffset	= colIndex - grid.colOrigin;

			return numLetterPlaced;
		}


		/// <summary>
		/// Places a letter on the grid at the give row/col index
		/// </summary>
		private bool PlaceLetter(Grid grid, char letter, int rowIndex, int colIndex)
		{
			// If there are not enough rows then add the amount of rows needed
			if (rowIndex >= grid.numRows)
			{
				AddRows(grid, rowIndex - grid.numRows + 1, false);
			}

			// If there are not enough cols then add the amount of cols needed
			if (colIndex >= grid.numCols)
			{
				AddCols(grid, colIndex - grid.numCols + 1, false);
			}

			GridCell gridCell = grid.gridCells[rowIndex][colIndex];

			if (gridCell.letter == (char)0)
			{
				gridCell.letter = letter;

				grid.placedLetters.Add(gridCell);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes the given number of cells from the end of the grid placed letters
		/// </summary>
		private void ClearPlacedLetters(Grid grid, int numToClear)
		{
			for (int i = 0; i < numToClear; i++)
			{
				int index = grid.placedLetters.Count - 1;

				GridCell gridCell = grid.placedLetters[index];

				// Clear the letter from the grid
				gridCell.canPlaceLetter	= true;
				gridCell.letter			= (char)0;

				grid.placedLetters.RemoveAt(index);
			}
		}

		/// <summary>
		/// Adds the given number of rows to the grid, if atBeginning is true then it inserts the rows at the beginning of the rows list
		/// </summary>
		private void AddRows(Grid grid, int numRows, bool atBeginning)
		{
			for (int i = 0; i < numRows; i++)
			{
				AddRow(grid, atBeginning);
			}
		}

		/// <summary>
		/// Adds the given number of cols to the grid, if atBeginning is true then it inserts the cols at the beginning of the rows
		/// </summary>
		private void AddCols(Grid grid, int numCols, bool atBeginning)
		{
			for (int i = 0; i < numCols; i++)
			{
				AddCol(grid, atBeginning);
			}
		}

		/// <summary>
		/// Adds a new row to the grid, if insertAtBeginning is true then it inserts the new row at the beginning of the rows list
		/// </summary>
		private void AddRow(Grid grid, bool insertAtBeginning)
		{
			// If we are inserting the row at the beginning of the list of rows then we need to increase the row origin
			if (insertAtBeginning)
			{
				grid.rowOrigin++;
			}

			// Get the row offset for each of the cells in the row
			int rowOriginOffset = insertAtBeginning ? -grid.rowOrigin : grid.numRows - grid.rowOrigin;

			List<GridCell> rowGridCells = new List<GridCell>();

			// Create all the GridCells for the row
			for (int i = 0; i < grid.numCols; i++)
			{
				rowGridCells.Add(CreateGridCell(rowOriginOffset, i - grid.colOrigin));
			}

			// Add the rows grid cells to the grid
			if (insertAtBeginning)
			{
				grid.gridCells.Insert(0, rowGridCells);
			}
			else
			{
				grid.gridCells.Add(rowGridCells);
			}

			// Increase the number of rows in the grid
			grid.numRows++;
		}

		/// <summary>
		/// Adds a new columns to the grid, if insertAtBeginning is true then it inserts the new cells are the beginning of each row instead of the end
		/// </summary>
		private void AddCol(Grid grid, bool insertAtBeginning)
		{
			// If we are inserting the column at the beginning then we need to increase the col origin
			if (insertAtBeginning)
			{
				grid.colOrigin++;
			}

			// Get the col offset for each of the cells in the column
			int colOriginOffset = insertAtBeginning ? -grid.colOrigin : grid.numCols - grid.colOrigin;

			// Create all the GridCells for the row
			for (int i = 0; i < grid.numRows; i++)
			{
				GridCell gridCell = CreateGridCell(i - grid.rowOrigin, colOriginOffset);

				// Add the grid cell to the row
				if (insertAtBeginning)
				{
					grid.gridCells[i].Insert(0, gridCell);
				}
				else
				{
					grid.gridCells[i].Add(gridCell);
				}
			}

			// Increase the number of cols in the grid
			grid.numCols++;
		}

		/// <summary>
		/// Creates all the possible boards using the given grid. 
		/// </summary>
		private void CreateBoards(Grid										grid,
		                          List<GridWord>							gridWords,
		                          Dictionary<char, List<GridWordLetter>>	letterMap,
		                          List<Board>								gridBoards)
		{
			if (grid.numPlacedWords >= gridWords.Count)
			{
				gridBoards.Add(CreateBoard(grid, gridWords));

				return;
			}

			// Try an place words on all the GridCells currently on the grid
			for (int i = 0; i < grid.placedLetters.Count; i++)
			{
				if (Stopping || gridBoards.Count >= currentMaxBoards)
				{
					return;
				}

				GridCell	gridCell = grid.placedLetters[i];
				int			rowIndex = GetRowIndex(grid, gridCell);
				int			colIndex = GetColIndex(grid, gridCell);

				List<GridWordLetter> gridWordLetters = GetPlaceableGridWordLetters(letterMap, gridCell.letter);

				if (gridWordLetters.Count > 0)
				{
					int			crossWorStartOffset;
					int			crossWordEndOffset;
					List<int>	crossLetterOffsets;

					if (!IsPartOfVerticalWord(grid, rowIndex, colIndex))
					{
						if (CanPlaceAnyWordVertically(grid, rowIndex, colIndex, out crossWorStartOffset, out crossWordEndOffset, out crossLetterOffsets))
						{
							TryPlaceWords(grid, gridWords, letterMap, gridBoards, Direction.Vertical, gridWordLetters, gridCell, crossWorStartOffset, crossWordEndOffset, crossLetterOffsets);
						}
					}
					else if (!IsPartOfHorizontalWord(grid, rowIndex, colIndex))
					{
						if (CanPlaceAnyWordHorizontally(grid, rowIndex, colIndex, out crossWorStartOffset, out crossWordEndOffset, out crossLetterOffsets))
						{
							TryPlaceWords(grid, gridWords, letterMap, gridBoards, Direction.Horizontal, gridWordLetters, gridCell, crossWorStartOffset, crossWordEndOffset, crossLetterOffsets);
						}
					}
				}
			}
		}

		/// <summary>
		/// Tries to place all the words in the gridWordLetters list on the given grid at the given row/col index
		/// </summary>
		private void TryPlaceWords(Grid										grid,
		                           List<GridWord>							gridWords,
		                           Dictionary<char, List<GridWordLetter>>	letterMap,
		                           List<Board>								gridBoards,
		                           Direction								direction,
		                           List<GridWordLetter>						gridWordLetters,
		                           GridCell									gridCell,
		                           int										crossWorStartOffset,
		                           int										crossWordEndOffset,
		                           List<int>								crossLetterOffsets)
		{
			for (int i = 0; i < gridWordLetters.Count; i++)
			{
				// Get a random grid word letter to use next
				int				randIndex		= random.Next(i, gridWordLetters.Count);
				GridWordLetter	gridWordLetter	= gridWordLetters[randIndex];

				// Swap the one we are going to use with the one at index i so next iteration of the for loop we do not pick it again
				gridWordLetters[randIndex]	= gridWordLetters[i];
				gridWordLetters[i]			= gridWordLetter;

				if (Stopping || gridBoards.Count >= currentMaxBoards)
				{
					return;
				}

				int	rowIndex = GetRowIndex(grid, gridCell);
				int	colIndex = GetColIndex(grid, gridCell);

				// Check if the word can be placed on the grid here, ie it fits on the board
				if (CanPlaceWord(grid, gridWordLetter, direction, rowIndex, colIndex, crossWorStartOffset, crossWordEndOffset, crossLetterOffsets))
				{
					// Get the start row/col index for the word
					int rowStartIndex = (direction == Direction.Vertical) ? rowIndex - gridWordLetter.index : rowIndex;
					int colStartIndex = (direction == Direction.Vertical) ? colIndex : colIndex - gridWordLetter.index;

					// Expand the number of rows if needed
					while (rowStartIndex < 0)
					{
						AddRow(grid, true);
						rowStartIndex++;
					}

					// Expand the number of columns if needed
					while (colStartIndex < 0)
					{
						AddCol(grid, true);
						colStartIndex++;
					}

					// Place the word on the board (We know it will fit at this point) and create all possible boards with this word here
					PlaceWordAndCreateBoards(grid, gridWords, letterMap, gridBoards, gridWordLetter.gridWord, rowStartIndex, colStartIndex, direction);
				}
			}
		}

		/// <summary>
		/// Checks if the word can be placed on the grid
		/// </summary>
		private bool CanPlaceWord(Grid grid, GridWordLetter gridWordLetter, Direction direction, int rowIndex, int colIndex, int crossWorStartOffset, int crossWordEndOffset, List<int> crossLetterOffsets)
		{
			int gridIndex = (direction == Direction.Vertical ? rowIndex : colIndex);

			GridWord	gridWord		= gridWordLetter.gridWord;
			int			prefixLen		= gridWordLetter.index;
			int			suffixLen		= gridWord.word.Length - gridWordLetter.index - 1;
			int			maxPrefixLen	= (crossWorStartOffset == -1) ? int.MaxValue : crossWorStartOffset;
			int			maxSuffixLen	= (crossWordEndOffset == -1) ? int.MaxValue : crossWordEndOffset;

			// Check if there are enough cells to fit the beginning anf end of the word. If the grid has a bonus word then dont allow expanding the board up or left
			if (prefixLen > maxPrefixLen || suffixLen > maxSuffixLen || (grid.hasBonusWord && gridIndex - prefixLen < 0))
			{
				return false;
			}

			for (int i = 0; i < crossLetterOffsets.Count; i++)
			{
				int crossLetterOffset	= crossLetterOffsets[i];
				int wordLetterIndex		= gridWordLetter.index + crossLetterOffset;

				// The word will end/start one cell away from the cross word
				if (wordLetterIndex == -1 || wordLetterIndex == gridWord.word.Length)
				{
					return false;
				}

				// Check if the word will overlap the cross word
				if (wordLetterIndex >= 0 && wordLetterIndex < gridWord.word.Length)
				{
					char wordLetter = gridWord.word[wordLetterIndex];
					char gridLetter = (direction == Direction.Vertical) ? grid.gridCells[crossLetterOffset][colIndex].letter : grid.gridCells[rowIndex][crossLetterOffset].letter;
				
					// If the letter in the word does not match the letter on the grid then the word cannot be placed
					if (wordLetter != gridLetter)
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if a word can be placed on the given GridCell vertically
		/// </summary>
		private bool CanPlaceAnyWordVertically(Grid grid, int rowIndex, int colIndex, out int crossWordStartOffset, out int crossWordEndOffset, out List<int> crossLetterOffsets)
		{
			crossWordStartOffset	= -1;
			crossWordEndOffset		= -1;
			crossLetterOffsets		= new List<int>();

			for (int i = rowIndex - 1; i >= 0; i--)
			{
				bool canPlace		= CanPlace(grid, i, colIndex);
				bool hasLetter		= HasLetter(grid, i, colIndex);
				bool leftLetter		= HasLetter(grid, i, colIndex - 1);
				bool rightLetter	= HasLetter(grid, i, colIndex + 1);
				bool topLetter		= HasLetter(grid, i - 1, colIndex);

				if (!topLetter && hasLetter && leftLetter && rightLetter)
				{
					crossLetterOffsets.Add(i - rowIndex);
				}
				else if (!canPlace || topLetter || hasLetter || leftLetter || rightLetter)
				{
					crossWordStartOffset = rowIndex - i - 1;

					break;
				}
			}

			for (int i = rowIndex + 1; i < grid.numRows; i++)
			{
				bool canPlace		= CanPlace(grid, i, colIndex);
				bool hasLetter		= HasLetter(grid, i, colIndex);
				bool leftLetter		= HasLetter(grid, i, colIndex - 1);
				bool rightLetter	= HasLetter(grid, i, colIndex + 1);
				bool bottomLetter	= HasLetter(grid, i + 1, colIndex);

				if (!bottomLetter && hasLetter && leftLetter && rightLetter)
				{
					crossLetterOffsets.Add(i - rowIndex);
				}
				else if (!canPlace || bottomLetter || hasLetter || leftLetter || rightLetter)
				{
					crossWordEndOffset = i - rowIndex - 1;

					break;
				}
			}

			return crossWordStartOffset != crossWordEndOffset || crossWordStartOffset == -1;
		}

		/// <summary>
		/// Checks if a word can be placed on the given GridCell horizontally
		/// </summary>
		private bool CanPlaceAnyWordHorizontally(Grid grid, int rowIndex, int colIndex, out int crossWordStartOffset, out int crossWordEndOffset, out List<int> crossLetterOffsets)
		{
			crossWordStartOffset	= -1;
			crossWordEndOffset		= -1;
			crossLetterOffsets		= new List<int>();

			for (int i = colIndex - 1; i >= 0; i--)
			{
				bool canPlace		= CanPlace(grid, rowIndex, i);
				bool hasLetter		= HasLetter(grid, rowIndex, i);
				bool topLetter		= HasLetter(grid, rowIndex - 1, i);
				bool bottomLetter	= HasLetter(grid, rowIndex + 1, i);
				bool leftLetter		= HasLetter(grid, rowIndex, i - 1);

				if (!leftLetter && hasLetter && topLetter && bottomLetter)
				{
					crossLetterOffsets.Add(i - colIndex);
				}
				else if (!canPlace || leftLetter || hasLetter || topLetter || bottomLetter)
				{
					crossWordStartOffset = colIndex - i - 1;

					break;
				}
			}

			for (int i = colIndex + 1; i < grid.numCols; i++)
			{
				bool canPlace		= CanPlace(grid, rowIndex, i);
				bool hasLetter		= HasLetter(grid, rowIndex, i);
				bool topLetter		= HasLetter(grid, rowIndex - 1, i);
				bool bottomLetter	= HasLetter(grid, rowIndex + 1, i);
				bool rightLetter	= HasLetter(grid, rowIndex, i + 1);

				if (!rightLetter && hasLetter && topLetter && bottomLetter)
				{
					crossLetterOffsets.Add(i - colIndex);
				}
				else if (!canPlace || rightLetter || hasLetter || topLetter || bottomLetter)
				{
					crossWordEndOffset = i - colIndex - 1;

					break;
				}
			}

			return crossWordStartOffset != crossWordEndOffset || crossWordStartOffset == -1;
		}

		/// <summary>
		/// Gets all GridWordLetter for words that have not been placed yet
		/// </summary>
		private List<GridWordLetter> GetPlaceableGridWordLetters(Dictionary<char, List<GridWordLetter>> letterMap, char letter)
		{
			List<GridWordLetter> gridWordLetters = letterMap.ContainsKey(letter) ? letterMap[letter] : null;
			List<GridWordLetter> placeableWords = new List<GridWordLetter>();

			for (int i = 0; i < gridWordLetters.Count; i++)
			{
				GridWordLetter gridWordLetter = gridWordLetters[i];

				// If the word has not been placed then add it to the list
				if (!gridWordLetter.gridWord.isPlaced)
				{
					placeableWords.Add(gridWordLetter);
				}
			}

			return placeableWords;
		}

		/// <summary>
		/// Gets the actual row index in grid.gridCells for the given GridCell
		/// </summary>
		private int GetRowIndex(Grid grid, GridCell gridCell)
		{
			return grid.rowOrigin + gridCell.rowOriginOffset;
		}

		/// <summary>
		/// Gets the actual column index in grid.gridCells for the given GridCell
		/// </summary>
		private int GetColIndex(Grid grid, GridCell gridCell)
		{
			return grid.colOrigin + gridCell.colOriginOffset;
		}

		/// <summary>
		/// Checks if the cell at the given index is part of a vertical word
		/// </summary>
		private bool IsPartOfVerticalWord(Grid grid, int rowIndex, int colIndex)
		{
			if (rowIndex - 1 >= 0 && grid.gridCells[rowIndex - 1][colIndex].letter != (char)0)
			{
				return true;
			}

			if (rowIndex + 1 < grid.numRows && grid.gridCells[rowIndex + 1][colIndex].letter != (char)0)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if the cell at the given index is part of a vertical word
		/// </summary>
		private bool IsPartOfHorizontalWord(Grid grid, int rowIndex, int colIndex)
		{
			if (colIndex - 1 >= 0 && grid.gridCells[rowIndex][colIndex - 1].letter != (char)0)
			{
				return true;
			}

			if (colIndex + 1 < grid.numCols && grid.gridCells[rowIndex][colIndex + 1].letter != (char)0)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if the cell at the given row/col index has a letter placed on it
		/// </summary>
		private bool HasLetter(Grid grid, int rowIndex, int colIndex)
		{
			return (rowIndex >= 0 && colIndex >= 0 && rowIndex < grid.numRows && colIndex < grid.numCols && grid.gridCells[rowIndex][colIndex].HasLetter);
		}

		/// <summary>
		/// Checks if the cell at the given row/col index has a letter placed on it
		/// </summary>
		private bool CanPlace(Grid grid, int rowIndex, int colIndex)
		{
			return (rowIndex >= 0 && colIndex >= 0 && rowIndex < grid.numRows && colIndex < grid.numCols && grid.gridCells[rowIndex][colIndex].canPlaceLetter);
		}

		/// <summary>
		/// Creates a finished Board
		/// </summary>
		private Board CreateBoard(Grid grid, List<GridWord> gridWords)
		{
			Board board = new Board();

			board.boardWords = new List<BoardWord>();

			// If there is a bonus word then set the start index to -1 so the first iteration of the for loop we add the bonus word
			int startIndex = (grid.bonusWord != null) ? -1 : 0;

			// Copy all the values from the GridWords to teh BoardWords
			for (int i = startIndex; i < gridWords.Count; i++)
			{
				GridWord	gridWord	= (i == -1 ? grid.bonusWord : gridWords[i]);
				BoardWord	boardWord	= new BoardWord();

				boardWord.word			= gridWord.word;
				boardWord.isVertical	= gridWord.isVertical;
				boardWord.rowIndex		= grid.rowOrigin + gridWord.rowOriginOffset;
				boardWord.colIndex		= grid.colOrigin + gridWord.colOriginOffset;

				board.boardWords.Add(boardWord);
			}

			// Adjust all the row/col indicies in the board so they start at 0,0 and set the number of rows/cols in the board
			NormalizeBoard(board);

			return board;
		}

		/// <summary>
		/// Sets all BoardWords row/col indicies so the board is as tight as possible
		/// </summary>
		private void NormalizeBoard(Board board)
		{
			int left	= int.MaxValue;
			int right	= int.MinValue;
			int top		= int.MaxValue;
			int bottom	= int.MinValue;

			for (int i = 0; i < board.boardWords.Count; i++)
			{
				BoardWord boardWord = board.boardWords[i];

				left	= Mathf.Min(left, boardWord.colIndex);
				right	= Mathf.Max(right, boardWord.colIndex + (boardWord.isVertical ? 0 : boardWord.word.Length - 1));
				top		= Mathf.Min(top, boardWord.rowIndex);
				bottom	= Mathf.Max(bottom, boardWord.rowIndex + (boardWord.isVertical ? boardWord.word.Length - 1 : 0));
			}

			board.numRows = bottom - top + 1;
			board.numCols = right - left + 1;

			for (int i = 0; i < board.boardWords.Count; i++)
			{
				BoardWord boardWord = board.boardWords[i];

				boardWord.rowIndex -= top;
				boardWord.colIndex -= left;
			}
		}

		/// <summary>
		/// Returns the best Board based on the boards size and the PreferredRatio
		/// </summary>
		private Board GetBestBoard(List<Board> boards)
		{
			if (boards.Count == 0)
			{
				return null;
			}

			// If the preferred ratio is 0 then just pick a random board
			if (PreferredRatio == 0)
			{
				return boards[random.Next(0, boards.Count)];
			}

			List<Board>	bestBoards	= new List<Board>() { boards[0] };
			float		bestValue	= CalculateValue(boards[0]);

			for (int i = 0; i < boards.Count; i++)
			{
				Board board			= boards[i];
				float boardValue	= CalculateValue(board);

				if (boardValue == bestValue)
				{
					bestBoards.Add(board);
				}
				else if (boardValue < bestValue)
				{
					bestBoards	= new List<Board>() { board };
					bestValue	= boardValue;
				}
			}

			return bestBoards.Count == 1 ? bestBoards[0] : bestBoards[random.Next(0, bestBoards.Count)];
		}

		/// <summary>
		/// Calculates the value used to determine what board is best, the smaller the value the better the board
		/// </summary>
		private float CalculateValue(Board board)
		{
			Vector2	size		= new Vector2(board.numRows, board.numCols);
			float	ratio		= size.x / size.y;
			float 	ratioDiff	= 1f + Mathf.Abs(PreferredRatio - ratio);

			return ratioDiff * (size.x * size.y);
		}

		/// <summary>
		/// Prints the grid cells
		/// </summary>
		private void PrintBoard(Board board)
		{
			char[][] letters = new char[board.numRows][];

			for (int i = 0; i < board.numRows; i++)
			{
				letters[i] = new char[board.numCols];

				for (int j = 0; j < board.numCols; j++)
				{
					letters[i][j] = '_';
				}
			}

			for (int i = 0; i < board.boardWords.Count; i++)
			{
				BoardWord boardWord = board.boardWords[i];

				for (int j = 0; j < boardWord.word.Length; j++)
				{
					int rowIndex = boardWord.rowIndex + (boardWord.isVertical ? j : 0);
					int colIndex = boardWord.colIndex + (boardWord.isVertical ? 0 : j);

					letters[rowIndex][colIndex] = boardWord.word[j];
				}
			}

			string print = "";

			for (int i = 0; i < board.numRows; i++)
			{
				for (int j = 0; j < board.numCols; j++)
				{
					print += letters[i][j];
				}

				print += "\n";
			}

			Debug.Log(print);
		}

		/// <summary>
		/// Prints the grid cells
		/// </summary>
		private void PrintBoardInfo(Board board)
		{
			string print = "";

			print += string.Format("rows:{0} cols:{1}", board.numRows, board.numCols);

			for (int i = 0; i < board.boardWords.Count; i++)
			{
				print += string.Format("\nword:{0}, rowIndex: {1}, colIndex: {2}", board.boardWords[i].word, board.boardWords[i].rowIndex, board.boardWords[i].colIndex);
			}

			Debug.Log(print);
		}

		/// <summary>
		/// Prints the grid cells
		/// </summary>
		private void DebugPrintGridCells(Grid grid)
		{
			string print = "";

			print += string.Format("r:{0} c:{1}\n", grid.numRows, grid.numCols);
			print += string.Format("ro:{0} co:{1}", grid.rowOrigin, grid.colOrigin);

			for (int i = 0; i < grid.gridCells.Count; i++)
			{
				print += "\n";

				for (int j = 0; j < grid.gridCells[i].Count; j++)
				{
					GridCell gridCell = grid.gridCells[i][j];

					if (gridCell.HasLetter)
					{
						print += gridCell.letter;
					}
					else if (!gridCell.canPlaceLetter)
					{
						print += "#";
					}
					else
					{
						print += "_";
					}
				}
			}

			Debug.Log(print);
		}

		/// <summary>
		/// Prints the grid cells
		/// </summary>
		private void DebugPrintRowOffsets(Grid grid)
		{
			string print = "";

			print += string.Format("r:{0} c:{1}\n", grid.numRows, grid.numCols);
			print += string.Format("ro:{0} co:{1}", grid.rowOrigin, grid.colOrigin);

			for (int i = 0; i < grid.gridCells.Count; i++)
			{
				print += "\n";

				for (int j = 0; j < grid.gridCells[i].Count; j++)
				{
					GridCell gridCell = grid.gridCells[i][j];

					print += gridCell.rowOriginOffset + "\t";
				}
			}

			Debug.Log(print);
		}

		/// <summary>
		/// Prints the grid cells
		/// </summary>
		private void DebugPrintColOffsets(Grid grid)
		{
			string print = "";

			print += string.Format("r:{0} c:{1}\n", grid.numRows, grid.numCols);
			print += string.Format("ro:{0} co:{1}", grid.rowOrigin, grid.colOrigin);

			for (int i = 0; i < grid.gridCells.Count; i++)
			{
				print += "\n";

				for (int j = 0; j < grid.gridCells[i].Count; j++)
				{
					GridCell gridCell = grid.gridCells[i][j];

					print += gridCell.colOriginOffset + "\t";
				}
			}

			Debug.Log(print);
		}

		#endregion
	}
}
