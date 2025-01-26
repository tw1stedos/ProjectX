using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordCross
{
	public abstract class WordBoard : UIMonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private bool			animateInCells		= true;
		[SerializeField] private float			animationDuration	= 0.5f;
		[SerializeField] private float			delayBetweenCells	= 0.01f;
		[SerializeField] private AnimationCurve	animationCurve		= null;

		#endregion

		#region Properties

		/// <summary>
		/// Invoked when a WordBoardCell is clicked
		/// </summary>
		public System.Action<int, int> OnWordBoardCellClicked { get; set;}

		#endregion

		#region Abstract Methods

		/// <summary>
		/// Initialize this board
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		/// Setup the board for the given level
		/// </summary>
		public abstract void Setup(ActiveLevel level);

		/// <summary>
		/// Clears the board of all UI elements
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Gets the list of WordBoardCell for the given word
		/// </summary>
		protected abstract List<WordBoardCell> GetWordGridCells(WordData wordData);

		#endregion

		#region Public Methods

		/// <summary>
		/// Shows the word on the board
		/// </summary>
		public virtual void ShowWord(WordData levelWordData)
		{
			ShowWord(levelWordData, true, true);
		}

		/// <summary>
		/// Shows the letter at the given index in the given word
		/// </summary>
		public virtual void ShowLetterForHint(WordData levelWordData, int index)
		{
			ShowLetterForHint(levelWordData, index, true, true);
		}

		/// <summary>
		/// Shows the word on the board
		/// </summary>
		public virtual void ShakeWord(WordData levelWordData)
		{
			StartCoroutine(ShakeWordOnBoard(levelWordData));
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Shows the found words and hints from the save data
		/// </summary>
		protected void ShowFoundWords(ActiveLevel level)
		{
			// Show all the letters for the words that have been found and hints that have been shown
			for (int i = 0; i < level.levelData.Words.Count; i++)
			{
				WordData wordData = level.levelData.Words[i];

				// If the word has been found then show it
				if (level.levelSaveData.foundWords.Contains(wordData.Word))
				{
					ShowWord(wordData, false, false);
				}
				// Else show any hints that have been shown
				else
				{
					List<bool> usedHints = level.levelSaveData.hintIndicesUsed[wordData.Word];

					for (int j = 0; j < usedHints.Count; j++)
					{
						// If the hint has been used then show the letter
						if (usedHints[j])
						{
							ShowLetterForHint(wordData, j, false, false);
						}
					}
				}
			}
		}

		/// <summary>
		/// Shows the given word on the board
		/// </summary>
		protected void ShowWord(WordData levelWordData, bool animate, bool awardCoins)
		{
			// Get the list of cells on the board for this word
			List<WordBoardCell> wordGridCells = GetWordGridCells(levelWordData);

			List<RectTransform> coinsToAward = new List<RectTransform>();

			for (int i = 0; i < wordGridCells.Count; i++)
			{
				WordBoardCell wordBoardCell = wordGridCells[i];

				// Check if we need to award a coin for this cell
				if (awardCoins && wordBoardCell.HasCoin && wordBoardCell.CurrentState == WordBoardCell.State.Blank)
				{
					coinsToAward.Add(wordBoardCell.coinObject.transform as RectTransform);
				}

				SetWordBoardState(wordBoardCell, WordBoardCell.State.Found, animate, (float)i * delayBetweenCells);
			}

			// Check if there are coins to award
			if (coinsToAward.Count > 0)
			{
				AwardCoins(coinsToAward);
			}
		}

		/// <summary>
		/// Shakes the words cells
		/// </summary>
		protected IEnumerator ShakeWordOnBoard(WordData levelWordData)
		{
			float	shakeAmount	= 10;
			float	shakeSpeed	= 0.05f;
			int		numShakes	= 5;
			float	shakeDir	= 1;

			List<WordBoardCell> boardCells = GetWordGridCells(levelWordData);

			for (int i = 0; i < numShakes; i++)
			{
				for (int j = 0; j < boardCells.Count; j++)
				{
					WordBoardCell boardCell = boardCells[j];

					float toPos = 0f;

					if (i < numShakes - 1)
					{
						toPos = shakeDir * shakeAmount * (j % 2 == 0 ? 1f : -1f);
					}

					if (levelWordData.IsVerticalOnBoard)
					{
						UIAnimation.PositionX(boardCell.transform as RectTransform, toPos, shakeSpeed).Play();
					}
					else
					{
						UIAnimation.PositionY(boardCell.transform as RectTransform, toPos, shakeSpeed).Play();
					}
				}

				shakeDir *= -1;

				yield return new WaitForSeconds(shakeSpeed);
			}
		}

		/// <summary>
		/// Shows the letter for hint.
		/// </summary>
		protected void ShowLetterForHint(WordData levelWordData, int index, bool animate, bool awardCoins)
		{
			List<WordBoardCell>	wordGridCells	= GetWordGridCells(levelWordData);
			WordBoardCell		wordBoardCell	= wordGridCells[index];

			if (awardCoins && wordBoardCell.HasCoin && wordBoardCell.CurrentState == WordBoardCell.State.Blank && CoinController.Exists())
			{
				AwardCoins(new List<RectTransform>() { wordBoardCell.coinObject.transform as RectTransform });
			}

			SetWordBoardState(wordBoardCell, WordBoardCell.State.Hint, animate, 0);
		}

		/// <summary>
		/// Sets the state on the WOrdBoardCell and animates it
		/// </summary>
		protected void SetWordBoardState(WordBoardCell wordBoardCell, WordBoardCell.State state, bool animate, float startDelay)
		{
			WordBoardCell.State fromState = wordBoardCell.CurrentState;

			if (fromState != WordBoardCell.State.Found)
			{
				// Update the state of the cell to hint
				wordBoardCell.SetState(state);

				// Check if we are suppose to animate in the cell
				if (animateInCells && animate)
				{
					AnimateCell(wordBoardCell, fromState, state, startDelay);
				}
			}
		}

		/// <summary>
		/// Aniamtes the cell based on the from and to states
		/// </summary>
		protected void AnimateCell(WordBoardCell wordBoardCell, WordBoardCell.State fromState, WordBoardCell.State toState, float startDelay)
		{
			if (fromState == WordBoardCell.State.Blank && toState == WordBoardCell.State.Hint)
			{
				FadeInLetterText(wordBoardCell, startDelay);
				ScaleLetterText(wordBoardCell, startDelay);
			}

			if (fromState == WordBoardCell.State.Blank && toState == WordBoardCell.State.Found)
			{
				FadeInLetterText(wordBoardCell, startDelay);
				FadeInLetterBackground(wordBoardCell, startDelay);
				ScaleLetterText(wordBoardCell, startDelay);
			}

			if (fromState == WordBoardCell.State.Hint && toState == WordBoardCell.State.Found)
			{
				FadeInLetterBackground(wordBoardCell, startDelay);
				FadeLetterTextColor(wordBoardCell, wordBoardCell.normalTextColor, wordBoardCell.foundTextColor, startDelay);
			}
		}

		/// <summary>
		/// Fades in the cells letter text
		/// </summary>
		protected void FadeInLetterText(WordBoardCell wordBoardCell, float startDelay)
		{
			Color fromColor = wordBoardCell.letterText.color;
			fromColor.a = 0f;

			Color toColor = wordBoardCell.letterText.color;
			toColor.a = 1f;

			UIAnimation anim = UIAnimation.Color(wordBoardCell.letterText, fromColor, toColor, animationDuration);
			SetupAndPlay(anim, startDelay);
		}

		/// <summary>
		/// Fades in the cells letter background
		/// </summary>
		protected void FadeInLetterBackground(WordBoardCell wordBoardCell, float startDelay)
		{
			Color fromColor = wordBoardCell.letterBackground.color;
			fromColor.a = 0f;

			Color toColor = wordBoardCell.letterBackground.color;
			toColor.a = 1f;

			UIAnimation anim = UIAnimation.Color(wordBoardCell.letterBackground, fromColor, toColor, animationDuration);
			SetupAndPlay(anim, startDelay);
		}

		/// <summary>
		/// Fades in the cells letter text
		/// </summary>
		protected void FadeLetterTextColor(WordBoardCell wordBoardCell, Color fromColor, Color toColor, float startDelay)
		{
			UIAnimation anim = UIAnimation.Color(wordBoardCell.letterText, fromColor, toColor, animationDuration);
			SetupAndPlay(anim, startDelay);
		}

		/// <summary>
		/// DOes the scale animation for the letter text
		/// </summary>
		protected void ScaleLetterText(WordBoardCell wordBoardCell, float startDelay)
		{
			UIAnimation anim;

			// Play the scale x animation
			anim = UIAnimation.ScaleX(wordBoardCell.letterText.transform as RectTransform, 0f, 1f, animationDuration);
			SetupAndPlay(anim, startDelay);

			// Play the scale y animation
			anim = UIAnimation.ScaleY(wordBoardCell.letterText.transform as RectTransform, 0f, 1f, animationDuration);
			SetupAndPlay(anim, startDelay);
		}

		protected void SetupAndPlay(UIAnimation anim, float startDelay)
		{
			anim.style				= UIAnimation.Style.Custom;
			anim.animationCurve		= animationCurve;
			anim.startDelay			= startDelay;
			anim.startOnFirstFrame	= true;
			anim.Play();
		}

		/// <summary>
		/// Awards a coin for each coinsToAward
		/// </summary>
		protected void AwardCoins(List<RectTransform> coinsToAward)
		{
			// Get the current amount of coins
			int animateFromCoins = GameController.Instance.Coins;

			// Give the amount of coins
			GameController.Instance.GiveCoins(coinsToAward.Count);

			// Get the amount of coins now after giving them
			int animateToCoins = GameController.Instance.Coins;

			// Animate the coins to the coin container
			CoinController.Instance.AnimateCoins(animateFromCoins, animateToCoins, coinsToAward);
		}

		#endregion
	}
}
