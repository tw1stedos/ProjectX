using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using dotmob;

namespace WordCross
{
	public class LetterWheel : UIMonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		#region Classes

		[System.Serializable]
		private class ScaleInfo
		{
			public int		letterCount			= 0;
			public float	letterScale			= 0;
			public float	rectTransformSize	= 0;
		}

		#endregion

		#region Enums

		private enum State
		{
			Idle,
			Selecting
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private UILine				uiLine				= null;
		[SerializeField] private LetterUI			letterUIPrefab		= null;
		[SerializeField] private float				selectThreshold		= 0f;
		[SerializeField] private bool				lineFollowsFinger	= false;
		[SerializeField] private Color				normalTextColor		= Color.red;
		[SerializeField] private Color				selectedTextColor	= Color.white;
		[SerializeField] private List<ScaleInfo>	scaleInfos 			= null;

		#endregion

		#region Member Variables

		private ObjectPool		letterUIPool;
		private List<LetterUI>	letterUIs;
		private List<LetterUI>	selectedLetterUIs;
		private State			state;
		private bool			isAnimatingLetters;
		private Camera			screenspaceCamera;

		#endregion

		#region Events

		public System.Action<string> OnWordSelected;
		public System.Action<string> OnSelectedLettersUpdated;

		#endregion

		#region Unity Methods

		public void OnPointerDown(PointerEventData eventData)
		{
			state = State.Selecting;

			UpdateDrag(eventData.position);
		}

		public void OnDrag(PointerEventData eventData)
		{
			UpdateDrag(eventData.position);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			state = State.Idle;

			string selectedWord = GetSelectedWord();

			if (OnWordSelected != null && !string.IsNullOrEmpty(selectedWord))
			{
				OnWordSelected(selectedWord);
			}

			ClearSelectedLetters();
		}

		#endregion

		#region Public Methods

		public void Initialize()
		{
			letterUIPool		= new ObjectPool(letterUIPrefab.gameObject, 3, ObjectPool.CreatePoolContainer(transform));
			letterUIs			= new List<LetterUI>();
			selectedLetterUIs	= new List<LetterUI>();

			Canvas canvas = Utilities.GetCanvas(transform);

			if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
			{
				screenspaceCamera = canvas.worldCamera;
			}
		}

		public void Setup(ActiveLevel level)
		{
			string	letters		= level.levelData.Letters;
			Color	packColor	= level.packInfo.color;

			ResetLetterWheel();

			// Set the color of the line
			uiLine.color = packColor;

			// Get the scale info
			ScaleInfo scaleInfo = GetScaleInfo(letters.Length);

			if (scaleInfo != null)
			{
				// Set the size of this letter wheels RectTransform based on the size set in the ScaleInfo
				RectT.sizeDelta = new Vector2(scaleInfo.rectTransformSize, scaleInfo.rectTransformSize);
			}

			CreateLetters(letters, packColor, scaleInfo);
		}

		public void Scramble()
		{
			Scramble(true);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Resets the letter wheel.
		/// </summary>
		private void ResetLetterWheel()
		{
			letterUIPool.ReturnAllObjectsToPool();
			letterUIs.Clear();
			selectedLetterUIs.Clear();
		}

		/// <summary>
		/// Gets the scale info for the given number of letters
		/// </summary>
		private ScaleInfo GetScaleInfo(int letterCount)
		{
			for (int i = 0; i < scaleInfos.Count; i++)
			{
				ScaleInfo scaleInfo = scaleInfos[i];

				if (letterCount == scaleInfo.letterCount)
				{
					return scaleInfo;
				}
			}

			return scaleInfos.Count > 0 ? scaleInfos[scaleInfos.Count - 1] : null;
		}

		/// <summary>
		/// Cerates and adds the letters to the letter whell
		/// </summary>
		private void CreateLetters(string characters, Color packColor, ScaleInfo scaleInfo)
		{
			float letterScale = (scaleInfo != null) ? scaleInfo.letterScale : 1;

			for (int i = 0; i < characters.Length; i++)
			{
				LetterUI letterUI					= letterUIPool.GetObject<LetterUI>(transform);
				letterUI.letterText.text			= characters[i].ToString();
				letterUI.letterText.color 			= normalTextColor;
				letterUI.selectedIndicator.color	= packColor;

				letterUI.transform.localScale = new Vector3(letterScale, letterScale, 1);

				letterUIs.Add(letterUI);
			}

			Scramble(false);
		}

		/// <summary>
		/// Returns a list of local positions for the letters in the letter wheel
		/// </summary>
		private List<Vector2> GetLetterPositions(int letterCount)
		{
			List<Vector2>	letterPositions	= new List<Vector2>();
			float			angleSpace		= 360f / (float)letterCount;
			float			radius			= Mathf.Min(RectT.rect.width, RectT.rect.height) / 2f;

			for (int i = 0; i < letterCount; i++)
			{
				letterPositions.Add(Vector2.up.Rotate(i * angleSpace).normalized * radius);
			}

			return letterPositions;
		}

		/// <summary>
		/// Called when the position of the players finger changes
		/// </summary>
		private void UpdateDrag(Vector2 screenPosition)
		{
			// Don't update while the letters are animating
			if (isAnimatingLetters)
			{
				return;
			}

			Vector2 localPosition;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(RectT, screenPosition, screenspaceCamera, out localPosition);

			UpdateSelectedLetters(localPosition);

			if (lineFollowsFinger)
			{
				UpdateLine(localPosition);
			}
		}

		/// <summary>
		/// Updates the list selected letters, adding any new ones that are close to the given position
		/// </summary>
		private void UpdateSelectedLetters(Vector2 localPosition)
		{
			for (int i = 0; i < letterUIs.Count; i++)
			{
				LetterUI letterUI = letterUIs[i];

				if (selectedLetterUIs.Contains(letterUI))
				{
					continue;
				}

				float distance = Vector2.Distance(localPosition, letterUI.RectT.anchoredPosition);

				if (distance <= selectThreshold)
				{
					AddSelectedLetter(letterUI);
				}
			}
		}

		/// <summary>
		/// Adds the selected letter
		/// </summary>
		private void AddSelectedLetter(LetterUI letterUI)
		{
			selectedLetterUIs.Add(letterUI);

			letterUI.letterText.color = selectedTextColor;
			letterUI.selectedIndicator.gameObject.SetActive(true);

			UpdateLine();

			if (OnSelectedLettersUpdated != null)
			{
				OnSelectedLettersUpdated(GetSelectedWord());
			}

			SoundManager.Instance.Play("letter-selected");
		}

		/// <summary>
		/// Updates the line using the positions of the selected LetterUIs
		/// </summary>
		private void UpdateLine()
		{
			List<Vector2> linePoints = GetSelectedLetterUIPositions();

			uiLine.SetPoints(linePoints);
		}

		/// <summary>
		/// Updates the line using the positions of the selected LetterUIs, adds the given point to the end of the line
		/// </summary>
		private void UpdateLine(Vector2 lastPosition)
		{
			List<Vector2> linePoints = GetSelectedLetterUIPositions();

			// Only add the last point if its far enough away from the last selected LetterUI
			if (linePoints.Count > 0 && Vector2.Distance(linePoints[linePoints.Count - 1], lastPosition) > selectThreshold)
			{
				linePoints.Add(lastPosition);
			}

			uiLine.SetPoints(linePoints);
		}

		/// <summary>
		/// Gets the local positions of all the selected LetterUIs
		/// </summary>
		private List<Vector2> GetSelectedLetterUIPositions()
		{
			List<Vector2> positions = new List<Vector2>();

			for (int i = 0; i < selectedLetterUIs.Count; i++)
			{
				positions.Add(selectedLetterUIs[i].transform.localPosition);
			}

			return positions;
		}

		/// <summary>
		/// Gets the string thats is create by appending all selected letters
		/// </summary>
		private string GetSelectedWord()
		{
			string word = "";

			for (int i = 0; i < selectedLetterUIs.Count; i++)
			{
				word += selectedLetterUIs[i].letterText.text;
			}

			return word;
		}

		/// <summary>
		/// Clears the list of selected letters and updates the line
		/// </summary>
		private void ClearSelectedLetters()
		{
			for (int i = 0; i < selectedLetterUIs.Count; i++)
			{
				selectedLetterUIs[i].letterText.color = normalTextColor;
				selectedLetterUIs[i].selectedIndicator.gameObject.SetActive(false);
			}

			selectedLetterUIs.Clear();

			UpdateLine();
		}

		private void Scramble(bool animate)
		{
			// Don't scramble the letters if the letters are being selected or they are already animating
			if (state != State.Idle || isAnimatingLetters)
			{
				return;
			}

			List<Vector2> letterPositions = PickRandomPositionsForLetters();

			if (animate)
			{
				StartCoroutine(AnimateLettersToPositions(letterPositions));
			}
			else
			{
				for (int i = 0; i < letterUIs.Count; i++)
				{
					LetterUI	letterUI = letterUIs[i];
					Vector2		position = letterPositions[i];

					letterUI.RectT.anchoredPosition = position;
				}
			}
		}

		private List<Vector2> PickRandomPositionsForLetters()
		{
			List<Vector2> letterPositions = GetLetterPositions(letterUIs.Count);

			if (letterPositions.Count > 1)
			{
				// Shuffle the letterPositions a couple times
				for (int i = 0; i < letterPositions.Count * 10; i++)
				{
					int index1 = Random.Range(0, letterPositions.Count);
					int index2 = Random.Range(0, letterPositions.Count);

					// Randomly swap the letter positions
					Swap(letterPositions, index1, index2);
				}

				// We want atleast one letter ui to actually swap positions so an easy way to do that is check if the first letter ui
				// will be in the same position and if so swap it with another random position
				if (Vector2.Distance(letterPositions[0], letterUIs[0].RectT.anchoredPosition) < 1f)
				{
					Swap(letterPositions, 0, Random.Range(1, letterPositions.Count));
				}
			}

			return letterPositions;
		}

		private void Swap(List<Vector2> letterPositions, int index1, int index2)
		{
			Vector2 tempPosition = letterPositions[index1];

			letterPositions[index1] = letterPositions[index2];
			letterPositions[index2]	= tempPosition;
		}

		private IEnumerator AnimateLettersToPositions(List<Vector2> letterPositions)
		{
			float animDuration = 0.35f;

			isAnimatingLetters = true;

			for (int i = 0; i < letterUIs.Count; i++)
			{
				LetterUI	letterUI = letterUIs[i];
				Vector2		position = letterPositions[i];

				UIAnimation anim;

				anim = UIAnimation.PositionX(letterUI.RectT, position.x, animDuration);
				anim.style = UIAnimation.Style.EaseOut;
				anim.Play();

				anim = UIAnimation.PositionY(letterUI.RectT, position.y, animDuration);
				anim.style = UIAnimation.Style.EaseOut;
				anim.Play();
			}

			yield return new WaitForSeconds(animDuration);

			isAnimatingLetters = false;
		}

		#endregion
	}
}
