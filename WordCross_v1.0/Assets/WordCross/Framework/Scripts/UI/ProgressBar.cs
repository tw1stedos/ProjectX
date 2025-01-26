using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob
{
	public class ProgressBar : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private RectTransform	barFillArea	= null;
		[SerializeField] private RectTransform	bar			= null;
		[SerializeField] private float			minSize		= 60;

		#endregion

		#region Public Methods

		public void SetProgress(float progress)
		{
			StartCoroutine(SetNextFrame(progress));
		}

		private IEnumerator SetNextFrame(float progress)
		{
			yield return new WaitForEndOfFrame();

			float fillWidth	= barFillArea.rect.width - minSize;
			float barWidth	= minSize + fillWidth * progress;

			bar.sizeDelta = new Vector2(barWidth, bar.sizeDelta.y);
		}

		#endregion
	}
}
