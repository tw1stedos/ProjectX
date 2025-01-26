using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordCross
{
	public class ExtraWords : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private Text extraWordsText = null;

		#endregion

		#region Public Methods

		public void SetNumExtraWordsFound(int value)
		{
			extraWordsText.text = value.ToString();
		}

		public void Shake()
		{
			StartCoroutine(ShakeContainer());
		}

		#endregion

		#region Private Methods

		private IEnumerator ShakeContainer()
		{
			float	shakeAmount	= 10;
			float	shakeSpeed	= 0.05f;
			int		numShakes	= 5;
			float	shakeDir	= 1;

			float origPos = (transform as RectTransform).anchoredPosition.x;

			for (int i = 0; i < numShakes; i++)
			{
				float toPos = origPos;

				if (i < numShakes - 1)
				{
					toPos += shakeDir * shakeAmount;
				}

				UIAnimation.PositionX(transform as RectTransform, toPos, shakeSpeed).Play();

				shakeDir *= -1;

				yield return new WaitForSeconds(shakeSpeed);
			}
		}

		#endregion
	}
}
