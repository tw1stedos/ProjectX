using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordCross
{
	public class SelectedLetters : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private GameObject	container			= null;
		[SerializeField] private Image		backgroundImage		= null;
		[SerializeField] private Text		selectedLettersText	= null; 

		#endregion

		#region Member Variables

		private const float ShakeAmount			= 10;
		private const float ShakeAnimDuration	= 0.05f;
		private const float FadeAnimDuration	= 0.25f;

		#endregion

		#region Public Methods

		public void SetSelectedLetters(string selectedLetters, Color packColor)
		{
			StopAllCoroutines();

			UIAnimation.DestroyAllAnimations(container);
			UIAnimation.DestroyAllAnimations(backgroundImage.gameObject);

			container.GetComponent<CanvasGroup>().alpha = 1f;

			container.SetActive(true);

			Color color = selectedLettersText.color;
			color.a = 1f;

			selectedLettersText.color = color;

			backgroundImage.color = packColor;

			selectedLettersText.text = selectedLetters;
		}

		public void SetCorrect()
		{
			FadeOutBackground();
			FadeUpText();
		}

		public void SetWrong()
		{
			StartCoroutine(Shake());
		}

		public void SetAlreadyFound()
		{
			FadeOutContainer();
		}

		public void SetExtraWord(RectTransform animateTo)
		{
			FadeOutBackground();
			FadeTextTo(animateTo);
		}

		#endregion

		#region Private Methods

		private IEnumerator Shake()
		{
			ShakeLeft(ShakeAnimDuration);

			yield return new WaitForSeconds(ShakeAnimDuration);

			ShakeRight(ShakeAnimDuration);

			yield return new WaitForSeconds(ShakeAnimDuration);

			ShakeLeft(ShakeAnimDuration);

			yield return new WaitForSeconds(ShakeAnimDuration);

			ShakeRight(ShakeAnimDuration);

			yield return new WaitForSeconds(ShakeAnimDuration);

			UIAnimation anim = UIAnimation.PositionX(container.transform as RectTransform, 0, ShakeAnimDuration);

			anim.Play();

			yield return new WaitForSeconds(ShakeAnimDuration);

			FadeOutContainer();
		}

		private void ShakeLeft(float animDuration)
		{
			UIAnimation anim = UIAnimation.PositionX(container.transform as RectTransform, -ShakeAmount, animDuration);

			anim.Play();
		}

		private void ShakeRight(float animDuration)
		{
			UIAnimation anim = UIAnimation.PositionX(container.transform as RectTransform, ShakeAmount, animDuration);

			anim.Play();
		}

		private void FadeOutContainer()
		{
			UIAnimation anim = UIAnimation.Alpha(container, 0f, FadeAnimDuration);

			anim.Play();
		}

		private void FadeOutBackground()
		{
			Color fromColor	= backgroundImage.color;
			Color toColor	= backgroundImage.color;

			toColor.a = 0f;

			UIAnimation anim = UIAnimation.Color(backgroundImage, fromColor, toColor, FadeAnimDuration);

			anim.Play();
		}

		/// <summary>
		/// Animates the selectedLettersText up while fading it out
		/// </summary>
		private void FadeUpText()
		{
			// Copy the selected letters text so we can animate it outside of the container
			Text selectTextCopy = CopyTextComponent();

			// Hide the original selected text
			Color color = selectedLettersText.color;
			color.a = 0f;

			selectedLettersText.color = color;

			// Fade out the selected text copy
			Color fromColor	= selectTextCopy.color;
			Color toColor	= selectTextCopy.color;

			toColor.a = 0f;

			UIAnimation anim;

			anim = UIAnimation.Color(selectTextCopy, fromColor, toColor, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseOut;
			anim.Play();

			// Move the selected text copy in an upwards motion
			RectTransform selectedTextCopyRectT = selectTextCopy.transform as RectTransform;

			float fromY	= selectedTextCopyRectT.anchoredPosition.y;
			float toY	= fromY + 100;

			anim = UIAnimation.PositionY(selectedTextCopyRectT, fromY, toY, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseOut;
			anim.Play();
		}

		/// <summary>
		/// Animates the selectedLettersText to the given RectTransforms positions while fading it out
		/// </summary>
		private void FadeTextTo(RectTransform animateTo)
		{
			// Copy the selected letters text so we can animate it outside of the container
			Text selectTextCopy = CopyTextComponent();

			// Hide the original selected text
			Color color = selectedLettersText.color;
			color.a = 0f;

			selectedLettersText.color = color;

			// Fade out the selected text copy
			Color fromColor	= selectTextCopy.color;
			Color toColor	= selectTextCopy.color;

			toColor.a = 0f;

			UIAnimation anim;

			anim = UIAnimation.Color(selectTextCopy, fromColor, toColor, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseIn;
			anim.Play();

			// Move the selected text copy
			Vector2 toPosition = Utilities.SwitchToRectTransform(animateTo, transform as RectTransform);

			anim = UIAnimation.PositionX(selectTextCopy.transform as RectTransform, toPosition.x, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseOut;
			anim.Play();

			anim = UIAnimation.PositionY(selectTextCopy.transform as RectTransform, toPosition.y, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseOut;
			anim.Play();

			// Scale them to 0
			anim = UIAnimation.ScaleX(selectTextCopy.transform as RectTransform, 0.2f, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseIn;
			anim.Play();

			anim = UIAnimation.ScaleY(selectTextCopy.transform as RectTransform, 0.2f, FadeAnimDuration);
			anim.style = UIAnimation.Style.EaseIn;
			anim.Play();
		}

		private Text CopyTextComponent()
		{
			Text copy = Instantiate(selectedLettersText, transform, true);

			ContentSizeFitter csf = copy.gameObject.AddComponent<ContentSizeFitter>();

			csf.verticalFit		= ContentSizeFitter.FitMode.PreferredSize;
			csf.horizontalFit	= ContentSizeFitter.FitMode.PreferredSize;

			return copy;
		}

		#endregion
	}
}
