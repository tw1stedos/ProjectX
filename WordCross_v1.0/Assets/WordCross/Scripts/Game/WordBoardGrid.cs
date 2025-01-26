using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordCross
{
	public class WordBoardGrid : WordBoard
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private float			maxCellSize			= 200;
		[SerializeField] private float			cellSpacing			= 10;
		[SerializeField] private WordBoardCell	wordBoardCellPrefab	= null;

		#endregion

		#region Member Variables

		private ObjectPool cellPlacementPool;
		private ObjectPool wordBoardCellPool;

		private GameObject					boardContainer;
		private float						gridCellSize;
		private List<List<GameObject>>		cellPlacements;
		private List<List<WordBoardCell>>	wordBoardCells;

		#endregion

		#region Public Methods

		/// <summary>
		/// Initialize this board
		/// </summary>
		public override void Initialize()
		{
			cellPlacements	= new List<List<GameObject>>();
			wordBoardCells	= new List<List<WordBoardCell>>();

			// Create a cell placement template GameObject to pass to the cellPlacementPool 
			Transform cellPlacementTemplate	= new GameObject("cell_template", typeof(RectTransform)).transform;
			cellPlacementTemplate.SetParent(transform, false);

			// Create a pool container to hold all the pooled objects
			Transform poolContainer = ObjectPool.CreatePoolContainer(transform, "pool_container");

			// Create the ObjectPools for the cell placement objects and the WordBoardCell objects
			cellPlacementPool = new ObjectPool(cellPlacementTemplate.gameObject, 9, poolContainer);
			wordBoardCellPool = new ObjectPool(wordBoardCellPrefab.gameObject, 9, poolContainer);

			// Create the container object that will hold all the cells
			boardContainer = new GameObject("board_container", typeof(GridLayoutGroup));
			boardContainer.transform.SetParent(transform, false);

			ContentSizeFitter contentSizeFitter	= boardContainer.AddComponent<ContentSizeFitter>();
			contentSizeFitter.verticalFit		= ContentSizeFitter.FitMode.PreferredSize;
			contentSizeFitter.horizontalFit		= ContentSizeFitter.FitMode.PreferredSize;
		}

		/// <summary>
		/// Setup the board for the given level
		/// </summary>
		public override void Setup(ActiveLevel level)
		{
			Clear();

			SetupGridGameObject(level.levelData);
			CreateCellPlacements(level.levelData);
			CreateWordGridCells(level);
			ShowFoundWords(level);
		}

		/// <summary>
		/// Clears the board of all UI elements
		/// </summary>
		public override void Clear()
		{
			cellPlacementPool.ReturnAllObjectsToPool();
			wordBoardCellPool.ReturnAllObjectsToPool();
			cellPlacements.Clear();
			wordBoardCells.Clear();
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Gets the WordGridCell for the given LevelWordData
		/// </summary>
		protected override List<WordBoardCell> GetWordGridCells(WordData wordData)
		{
			string word = wordData.Word;

			List<WordBoardCell> cells = new List<WordBoardCell>();

			for (int i = wordData.BoardRowStartIndex; i <= wordData.BoardRowEndIndex; i++)
			{
				for (int j = wordData.BoardColStartIndex; j <= wordData.BoardColEndIndex; j++)
				{
					cells.Add(wordBoardCells[i][j]);
				}
			}

			return cells;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the cell size on the GridLayoutGroup so that all cells will fit in the WordCrid
		/// </summary>
		private void SetupGridGameObject(LevelData levelData)
		{
			int		rows	= levelData.BoardRows;
			int		cols	= levelData.BoardCols;
			float	height	= RectT.rect.height - (float)(rows - 1) * cellSpacing;
			float	width	= RectT.rect.width - (float)(cols - 1) * cellSpacing;

			gridCellSize = Mathf.Min(maxCellSize, height / (float)rows, width / (float)cols);

			GridLayoutGroup	gridLayoutGroup = boardContainer.GetComponent<GridLayoutGroup>();
			gridLayoutGroup.constraint		= GridLayoutGroup.Constraint.FixedColumnCount;
			gridLayoutGroup.constraintCount	= cols;
			gridLayoutGroup.spacing			= new Vector2(cellSpacing, cellSpacing);
			gridLayoutGroup.cellSize		= new Vector2(gridCellSize, gridCellSize);
		}

		/// <summary>
		/// Creates the GameObjects that will act as a parent to the WordGridCell
		/// </summary>
		private void CreateCellPlacements(LevelData levelData)
		{
			for (int i = 0; i < levelData.BoardRows; i++)
			{
				List<GameObject> row = new List<GameObject>();

				for (int j = 0; j < levelData.BoardCols; j++)
				{
					GameObject cellGameObject	= cellPlacementPool.GetObject(boardContainer.transform);
					cellGameObject.name			= string.Format("cell_{0}_{1}", i, j);
					row.Add(cellGameObject);
				}

				cellPlacements.Add(row);
			}
		}

		/// <summary>
		/// Creates the WordCridCell objects and places them in the correct cell GameObject
		/// </summary>
		private void CreateWordGridCells(ActiveLevel level)
		{
			CreateEmptyWordGridCellsList(level.levelData);

			for (int i = 0; i < level.levelData.Words.Count; i++)
			{
				WordData wordData = level.levelData.Words[i];

				CreateWordGridCells(level, wordData);
			}
		}

		/// <summary>
		/// Adds null values to wordGridCells
		/// </summary>
		private void CreateEmptyWordGridCellsList(LevelData levelData)
		{
			for (int i = 0; i < levelData.BoardRows; i++)
			{
				List<WordBoardCell> row = new List<WordBoardCell>();

				for (int j = 0; j < levelData.BoardCols; j++)
				{
					row.Add(null);
				}

				wordBoardCells.Add(row);
			}
		}

		/// <summary>
		/// Creates the WordGridCell objects for each letter in the given word
		/// </summary>
		private void CreateWordGridCells(ActiveLevel level, WordData wordData)
		{
			string	word		= wordData.Word;
			int		letterIndex	= 0;

			for (int i = wordData.BoardRowStartIndex; i <= wordData.BoardRowEndIndex; i++)
			{
				for (int j = wordData.BoardColStartIndex; j <= wordData.BoardColEndIndex; j++)
				{
					WordBoardCell wordBoardCell = CreateWordGridCell(i, j);

					// Setup WordGridCell UI
					wordBoardCell.letterText.text			= word[letterIndex].ToString();
					wordBoardCell.letterBackground.color	= level.packInfo.color;

					// If the cell doesn't already have a coin on it then check if we need to assign a coin to it
					if (!wordBoardCell.HasCoin)
					{
						wordBoardCell.HasCoin = wordData.CoinIndices.Contains(letterIndex) && !GameController.Instance.IsLevelCompleted(level.levelData);
						wordBoardCell.coinObject.SetActive(wordBoardCell.HasCoin);
					}

					letterIndex++;
				}
			}
		}

		/// <summary>
		/// Creates a WordGridCell at the given row / col if there is not already one there
		/// </summary>
		private WordBoardCell CreateWordGridCell(int row, int col)
		{
			if (wordBoardCells[row][col] == null)
			{
				GameObject		cellGameObject	= cellPlacements[row][col];
				WordBoardCell	wordBoardCell	= wordBoardCellPool.GetObject<WordBoardCell>(cellGameObject.transform);

				wordBoardCell.Row			= row;
				wordBoardCell.Col			= col;
				wordBoardCell.HasCoin		= false;
				wordBoardCell.OnCellClicked = OnWordBoardCellClicked;

				wordBoardCell.SetState(WordBoardCell.State.Blank);

				// Set the scale of the WordGridCell so it fills the cell of the grid
				float size	= Mathf.Max(wordBoardCell.RectT.rect.width, wordBoardCell.RectT.rect.height);
				float scale	= gridCellSize / size;

				wordBoardCell.transform.localScale = new Vector3(scale, scale, 1f);

				// Destroy any animations on the object that might be left over from a previous game
				UIAnimation.DestroyAllAnimations(wordBoardCell.letterText.gameObject);
				UIAnimation.DestroyAllAnimations(wordBoardCell.letterBackground.gameObject);

				wordBoardCells[row][col] = wordBoardCell;
			}

			return wordBoardCells[row][col];
		}

		#endregion
	}
}
