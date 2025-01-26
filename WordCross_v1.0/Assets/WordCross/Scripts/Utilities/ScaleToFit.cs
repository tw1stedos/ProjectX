using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordCross
{
	/// <summary>
	/// Scales this objects RectTransform to fit it's parents
	/// </summary>
	public class ScaleToFit : UIBehaviour
	{
		#region Unity Methods

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();

			RectTransform rectT			= transform as RectTransform;
			RectTransform parentRectT	= transform.parent as RectTransform;

			if (rectT != null && parentRectT != null)
			{
				float xScale	= parentRectT.rect.width / rectT.rect.width;
				float yScale	= parentRectT.rect.height / rectT.rect.height;
				float scale		= Mathf.Min(1f, xScale, yScale);

				transform.localScale = new Vector3(scale, scale, 1f);
			}
		}

		#endregion
	}
}
