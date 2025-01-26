using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using dotmob;

namespace WordCross
{
	public class UIController : SingletonComponent<UIController>
	{
		#region Inspector Variables

		[SerializeField] private Text			gamePointsText			= null;
		[SerializeField] private GameObject		playButton				= null;
		[SerializeField] private Text			playButtonText			= null;
		[SerializeField] private Image			backgroundImage			= null;
		[SerializeField] private Sprite			mainBackgroundSprite	= null;
		[SerializeField] private float			backgroundFadeDuration	= 0.35f;
		[Space]
		[SerializeField] private PackItemUI		packItemUIPrefab	= null;
		[SerializeField] private CategoryItemUI categoryItemUI		= null;
		[SerializeField] private Transform		packListContainer	= null;
		[Space]
		[SerializeField] private LevelItemUI	levelItemUIPrefab	= null;
		[SerializeField] private Transform		levelListContainer	= null;
		[Space]
		[SerializeField] private Text			coinsPerHintText		= null;
		[SerializeField] private Text			coinsPerMultiHintText	= null;
		[SerializeField] private Text			coinsPerTargetHintText	= null;
		[Space]
		[SerializeField] private Color			hintSelectIconNormalColor	= Color.white;
		[SerializeField] private Color			hintSelectIconActiveColor	= Color.white;
		[SerializeField] private Image			hintSelectIcon				= null;
		[SerializeField] private GameObject		hintSelectOverlay			= null;
		[Space]
		[SerializeField] private GameObject		topBarBackButton		= null;
		[SerializeField] private Text			topBarPackText			= null;
		[SerializeField] private Text			topBarCategoryText		= null;
		[SerializeField] private Text			topBarLevelText			= null;
		[SerializeField] private float			topBarAnimDuration		= 0.35f;

		#endregion

		#region Member Variables

		private ObjectPool packItemUIPool;
		private ObjectPool categoryItemUIPool;
		private ObjectPool levelItemUIPool;

		private PackInfo		selectedPackInfo;
		private CategoryInfo	selectedCategoryInfo;

		private Image backgroundCopy;

		#endregion

		#region Unity Methods

		private void Start()
		{
			packItemUIPool		= new ObjectPool(packItemUIPrefab.gameObject, 1, ObjectPool.CreatePoolContainer(transform, "pack_item_pool_container"));
			categoryItemUIPool	= new ObjectPool(categoryItemUI.gameObject, 1, ObjectPool.CreatePoolContainer(transform, "category_item_pool_container"));
			levelItemUIPool		= new ObjectPool(levelItemUIPrefab.gameObject, 1, ObjectPool.CreatePoolContainer(transform, "level_item_pool_container"));

			// Check if there is a current active level
			if (GameController.Instance.CurrentActiveLevel != null)
			{
				// Set the selected pack and category
				selectedPackInfo		= GameController.Instance.CurrentActiveLevel.packInfo;
				selectedCategoryInfo	= GameController.Instance.CurrentActiveLevel.categoryInfo;
			}

			gamePointsText.text			= GameController.Instance.GamePoints.ToString();
			coinsPerHintText.text		= GameController.Instance.CoinCostPerHint.ToString();
			coinsPerMultiHintText.text	= GameController.Instance.CoinCostPerMultiHint.ToString();
			coinsPerTargetHintText.text	= GameController.Instance.CoinCostPerTargetHint.ToString();

			UpdateUI();

			// Remove the play button if all the levels have been completed
			playButton.SetActive(!GameController.Instance.IsLastLevelInGameCompleted());

			ScreenManager.Instance.OnShowingScreen		+= OnScreenShowing;
			ScreenManager.Instance.OnSwitchingScreens	+= OnSwitchingScreens;
		}

		#endregion

		#region Public Methods

		public void UpdateUI()
		{
			if (!GameController.Instance.IsLastLevelInGameCompleted())
			{
				// Set the main screens play button text
				playButtonText.text = string.Format("Level {0}", GameController.Instance.LastCompletedLevelNumber + 1);
			}

			UpdatePackListItems();
			UpdateLevelListItems();
		}

		public void OnNewLevelStarted()
		{
			// Set the selected pack and category to the level that is currently being played so when the level screen is show it shows the correct levels
			selectedPackInfo		= GameController.Instance.CurrentActiveLevel.packInfo;
			selectedCategoryInfo	= GameController.Instance.CurrentActiveLevel.categoryInfo;

			// Set the new level text on the top bar
			topBarLevelText.text = "Level " + GameController.Instance.CurrentActiveLevel.levelData.GameLevelNumber;

			// Make sure the correct background is being displayed
			SetBackground(selectedPackInfo.background);

			UpdateUI();
		}

		/// <summary>
		/// Invoked when the play button one the main screen is clicked
		/// </summary>
		public void OnMainScreenPlayClicked()
		{
			GameController.Instance.StartLevel(GameController.Instance.LastCompletedLevelNumber + 1);

			ScreenManager.Instance.Show("game");
		}

		public void UpdatePlayerSelectingHint()
		{
			bool isSelecting = GameController.Instance.PlayerSelectingHint;

			hintSelectIcon.color = isSelecting ? hintSelectIconActiveColor : hintSelectIconNormalColor;

			hintSelectOverlay.SetActive(isSelecting);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates the pack list
		/// </summary>
		private void UpdatePackListItems()
		{
			// Clear the pack list of all items
			packItemUIPool.ReturnAllObjectsToPool();
			categoryItemUIPool.ReturnAllObjectsToPool();

			// Create a new PackItemUI for each pack info in the GameController
			for (int i = 0; i < GameController.Instance.PackInfos.Count; i++)
			{
				PackInfo	packInfo	= GameController.Instance.PackInfos[i];
				PackItemUI	packItemUI	= packItemUIPool.GetObject<PackItemUI>(packListContainer);

				packItemUI.Setup(packInfo, categoryItemUIPool);

				packItemUI.OnCategorySelected = OnCategorySelected;
			}
		}

		/// <summary>
		/// Invoked when a category inside a pack is clicked
		/// </summary>
		private void OnCategorySelected(PackInfo packInfo, int categoryIndex)
		{
			// Set the selected CategoryInfo to display on the level screen
			selectedPackInfo		= packInfo;
			selectedCategoryInfo	= packInfo.categoryInfos[categoryIndex];

			// Set the background is being displayed
			SetBackground(selectedPackInfo.background);

			// Setup the level screen with the new selected Category Info
			UpdateLevelListItems();

			ScreenManager.Instance.Show("level");
		}

		/// <summary>
		/// Creates a level list using the selected category info
		/// </summary>
		private void UpdateLevelListItems()
		{
			// Clear the level list of all items
			levelItemUIPool.ReturnAllObjectsToPool();

			if (selectedPackInfo != null && selectedCategoryInfo != null)
			{
				// Create a new LevelItemUI for each level in the selected category
				for (int i = 0; i < selectedCategoryInfo.LevelDatas.Count; i++)
				{
					LevelData	levelData	= selectedCategoryInfo.LevelDatas[i];
					LevelItemUI	levelItemUI	= levelItemUIPool.GetObject<LevelItemUI>(levelListContainer);

					levelItemUI.Setup(levelData, selectedPackInfo.color);

					levelItemUI.Index				= i;
					levelItemUI.OnListItemClicked	= OnLevelSelected;
				}
			}
		}

		/// <summary>
		/// Invoked when a level is selected
		/// </summary>
		private void OnLevelSelected(int index, object data)
		{
			// Start the level in the GameController
			GameController.Instance.StartLevel(selectedPackInfo, selectedCategoryInfo, index);

			// Show the game screen
			ScreenManager.Instance.Show("game");
		}

		/// <summary>
		/// Sets the background.
		/// </summary>
		private void SetBackground(Sprite backgroundSprite)
		{
			Sprite currentBackground = (backgroundCopy == null) ? backgroundImage.sprite : backgroundCopy.sprite;

			// Already showing the given background so return
			if (currentBackground == backgroundSprite)
			{
				return;
			}

			if (backgroundCopy == null)
			{
				// If there is not already a backgroundCopy created then create one noew
				backgroundCopy = Instantiate(backgroundImage, backgroundImage.transform, true);
			}
			else
			{
				// If there is a backgrpound copy then it will be used to display the current background which will fade in so set
				// the original background to the current sprite
				backgroundImage.sprite = backgroundCopy.sprite;
			}

			// Set the sprite on the background copy
			backgroundCopy.sprite = backgroundSprite;

			// Fade in the new background
			UIAnimation anim = UIAnimation.Color(backgroundCopy, new Color(1f, 1f, 1f, 0f), Color.white, backgroundFadeDuration);
			anim.startOnFirstFrame = true;
			anim.Play();
		}

		/// <summary>
		/// Invoked when a new screen is showing in ScreenController
		/// </summary>
		private void OnScreenShowing(string screenId)
		{
			if (screenId == ScreenManager.Instance.HomeScreenId || screenId == "pack")
			{
				SetBackground(mainBackgroundSprite);

				gamePointsText.text = GameController.Instance.GamePoints.ToString();

				// Remove the play button if all the levels have been completed
				playButton.SetActive(!GameController.Instance.IsLastLevelInGameCompleted());
			}
		}

		/// <summary>
		/// Invoked when a new screen is showing in ScreenController
		/// </summary>
		private void OnSwitchingScreens(string fromScreenId, string toScreenId)
		{
			if (toScreenId == ScreenManager.Instance.HomeScreenId)
			{
				// Fade out the top bar
				PlayTopBarAnimation(UIAnimation.Alpha(topBarBackButton, 1f, 0f, topBarAnimDuration));
			}
			else if (fromScreenId == ScreenManager.Instance.HomeScreenId)
			{
				// Fade in the top bar
				PlayTopBarAnimation(UIAnimation.Alpha(topBarBackButton, 0f, 1f, topBarAnimDuration));
			}

			if (toScreenId == "level")
			{
				topBarPackText.text		= selectedPackInfo.packName;
				topBarCategoryText.text	= selectedCategoryInfo.displayName;

				// Fade in the pack/category text
				PlayTopBarAnimation(UIAnimation.Alpha(topBarPackText.gameObject, 0f, 1f, topBarAnimDuration));
				PlayTopBarAnimation(UIAnimation.Alpha(topBarCategoryText.gameObject, 0f, 1f, topBarAnimDuration));
			}
			else if (fromScreenId == "level")
			{
				// Fade out the pack/category text
				PlayTopBarAnimation(UIAnimation.Alpha(topBarPackText.gameObject, 1f, 0f, topBarAnimDuration));
				PlayTopBarAnimation(UIAnimation.Alpha(topBarCategoryText.gameObject, 1f, 0f, topBarAnimDuration));
			}

			if (toScreenId == "game")
			{
				topBarLevelText.text = "Level " + GameController.Instance.CurrentActiveLevel.levelData.GameLevelNumber;

				// Fade in the level text
				PlayTopBarAnimation(UIAnimation.Alpha(topBarLevelText.gameObject, 0f, 1f, topBarAnimDuration));
			}
			else if (fromScreenId == "game")
			{
				// Fade out the level text
				PlayTopBarAnimation(UIAnimation.Alpha(topBarLevelText.gameObject, 1f, 0f, topBarAnimDuration));
			}
		}

		private void PlayTopBarAnimation(UIAnimation anim)
		{
			anim.style				= UIAnimation.Style.EaseOut;
			anim.startOnFirstFrame	= true;
			anim.Play();
		}

		#endregion
	}
}
