using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordCross
{
	public class LevelItemUI : ClickableListItem
	{
		#region Classes

		[System.Serializable]
		private class LevelPreviewSetting
		{
			public int		letterCount	= 0;
			public float	radius		= 0;
			public int		fontSize	= 0;
			public float	yPos		= 0;
		}

		#endregion

		#region Member Variables

		private bool	initialized;
		private float	defaultRadius;
		private int		defaultFontSize;
		private float	defaultYPos;

		#endregion

		#region Inspector Variables

		[SerializeField] private Text						displayNameText			= null;
		[SerializeField] private GameObject					lockedIndicator			= null;
		[SerializeField] private LevelPreviewText			levelPreview			= null;
		[SerializeField] private string						currentLevelAnimationId	= "";
		[SerializeField] private List<LevelPreviewSetting>	letterPreviewSettings	= null;
		[SerializeField] private List<Graphic>				coloredGraphics			= null;

		#endregion

		#region Public Methods

		public void Setup(LevelData levelData, Color packColor)
		{
			if (!initialized)
			{
				Initialize();
			}

			bool isLocked		= GameController.Instance.IsLevelLocked(levelData);
			bool isCompleted	= GameController.Instance.IsLevelCompleted(levelData);

			// If the level is locked then show the locked indicator and hide the letters preview
			lockedIndicator.SetActive(isLocked);
			levelPreview.gameObject.SetActive(!isLocked);

			// Set the button interactable to false if the level is locked
			UIButton.interactable = !isLocked;

			displayNameText.text	= string.Format("Level {0}", levelData.GameLevelNumber);
			levelPreview.text		= levelData.Letters;

			// Set the font size and radius of the LevelPreviewText based on how many letters there are in the level
			LevelPreviewSetting levelPreviewSetting = GetLevelPreviewSetting(levelData.Letters.Length);
			
			levelPreview.radius		= (levelPreviewSetting == null) ? defaultRadius : levelPreviewSetting.radius;
			levelPreview.fontSize	= (levelPreviewSetting == null) ? defaultFontSize :levelPreviewSetting.fontSize;

			// Set the y position of the LevelPreviewText based on how many letters are in it
			RectTransform	levelPreviewRectT	= levelPreview.transform as RectTransform;
			float			yPos				= (levelPreviewSetting == null) ? defaultYPos :levelPreviewSetting.yPos;

			levelPreviewRectT.anchoredPosition = new Vector2(levelPreviewRectT.anchoredPosition.x, yPos);

			// Set the color of all the graphics
			for (int i = 0; i < coloredGraphics.Count; i++)
			{
				coloredGraphics[i].color = packColor;
			}

			// If the level is not locked and not completed then it is the current level
			if (!isCompleted && !isLocked && !GameController.Instance.DebugDisableLocking)
			{
				UIAnimation.PlayAllById(gameObject, currentLevelAnimationId);
			}
			else
			{
				UIAnimation.StopAllById(gameObject, currentLevelAnimationId);

				// The animations change the scale so we need to make sure it's set back to one
				transform.localScale = Vector3.one;
			}
		}

		#endregion

		#region Private Methods

		private void Initialize()
		{
			initialized		= true;
			defaultRadius	= levelPreview.radius;
			defaultFontSize	= levelPreview.fontSize;
			defaultYPos		= (levelPreview.transform as RectTransform).anchoredPosition.y;
		}

		private LevelPreviewSetting GetLevelPreviewSetting(int letterCount)
		{
			for (int i = 0; i < letterPreviewSettings.Count; i++)
			{
				LevelPreviewSetting levelPreviewSetting = letterPreviewSettings[i];

				if (letterCount == levelPreviewSetting.letterCount)
				{
					return levelPreviewSetting;
				}
			}

			return null;
		}

		#endregion
	}
}
