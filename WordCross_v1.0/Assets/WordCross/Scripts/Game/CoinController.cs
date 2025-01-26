using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using dotmob;

namespace WordCross
{
	public class CoinController : SingletonComponent<CoinController>
	{
		#region Inspector Variables

		[SerializeField] private Text			coinsText;
		[SerializeField] private RectTransform	animateTo;
		[SerializeField] private RectTransform	animationContainer;
		[SerializeField] private RectTransform	coinPrefab;
		[SerializeField] private float			animationDuration;
		[SerializeField] private float			delayBetweenCoins;

		#endregion

		#region Member Variables

		private ObjectPool coinPool;

		#endregion

		#region Unity Methods

		private void Start()
		{
			coinPool = new ObjectPool(coinPrefab.gameObject, 1, animationContainer);
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Increments the Coins by the give amount
		/// </summary>
		public void SetCoinsText(int coins)
		{
			coinsText.text = coins.ToString();
		}

		/// <summary>
		/// Animates coins to the coin container
		/// </summary>
		public void AnimateCoins(int fromCoinAmount, int toCoinAmount, List<RectTransform> fromRects)
		{
			SoundManager.Instance.Play("coins-awarded");

			for (int i = 0; i < fromRects.Count; i++)
			{
				// Animate each coin separately and when the coin reaches the coin container set the text coin amount
				int setCoinsTextTo = (int)Mathf.Lerp(fromCoinAmount, toCoinAmount, (i + 1) / fromRects.Count);

				AnimateCoin(fromRects[i], (float)i * delayBetweenCoins, setCoinsTextTo);
			}
		}

		#endregion

		#region Private Methods

		private void AnimateCoin(RectTransform coinRectTransform, float startDelay, int setCoinAmountTextTo)
		{
			RectTransform coinToAnimate = coinPool.GetObject<RectTransform>();

			UIAnimation.DestroyAllAnimations(coinToAnimate.gameObject);

			// Need to set the scale of coinToAnimate to the same scale as coinRectTransform
			coinToAnimate.SetParent(coinRectTransform.parent, false);
			coinToAnimate.sizeDelta			= coinRectTransform.sizeDelta;
			coinToAnimate.localScale		= coinRectTransform.localScale;
			coinToAnimate.anchoredPosition	= coinRectTransform.anchoredPosition;
			coinToAnimate.SetParent(animationContainer);

			Vector2 animateToPosition = Utilities.SwitchToRectTransform(animateTo, animationContainer);

			// Aniamte the x position position of the coin
			PlayAnimation(UIAnimation.PositionX(coinToAnimate, animateToPosition.x, animationDuration), startDelay);

			// Aniamte the y position position of the coin
			PlayAnimation(UIAnimation.PositionY(coinToAnimate, animateToPosition.y, animationDuration), startDelay);

			// Animate the x scale
			PlayAnimation(UIAnimation.ScaleX(coinToAnimate, 1, animationDuration), startDelay);

			// Animate the y scale
			PlayAnimation(UIAnimation.ScaleY(coinToAnimate, 1, animationDuration), startDelay);

			// Animate the width
			PlayAnimation(UIAnimation.Width(coinToAnimate, animateTo.sizeDelta.x, animationDuration), startDelay);

			// Animate the height
			PlayAnimation(UIAnimation.Height(coinToAnimate, animateTo.sizeDelta.y, animationDuration), startDelay);

			StartCoroutine(WaitThenSetCoinsText(setCoinAmountTextTo, animationDuration + startDelay));
		}

		/// <summary>
		/// Sets up and plays the UIAnimation for a coin
		/// </summary>
		private void PlayAnimation(UIAnimation anim, float startDelay)
		{
			anim.style				= UIAnimation.Style.EaseOut;
			anim.startDelay			= startDelay;
			anim.startOnFirstFrame	= true;

			anim.OnAnimationFinished += (GameObject target) =>
			{
				coinPool.ReturnObjectToPool(target);
			};

			anim.Play();
		}

		private IEnumerator WaitThenSetCoinsText(int coinAmount, float waitTime)
		{
			yield return new WaitForSeconds(waitTime);

			SetCoinsText(coinAmount);
		}

		#endregion
	}
}
