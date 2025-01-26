using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordCross
{
	[ExecuteInEditMode]
	public class LevelPreviewText : Text
	{
		#region Properties

		public float radius;

		#endregion

		#region Protected Variables

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			base.OnPopulateMesh(toFill);

			List<UIVertex> stream = new List<UIVertex>();
			List<UIVertex> newStream = new List<UIVertex>();

			toFill.GetUIVertexStream(stream);

			float letterCount	= text.Length;
			float rotationAngle	= 360f / letterCount;

			for (int i = 0; i < stream.Count; i += 6)
			{
				// Get the 6 verts that make up the single letter
				UIVertex vert1 = stream[i];
				UIVertex vert2 = stream[i + 1];
				UIVertex vert3 = stream[i + 2];
				UIVertex vert4 = stream[i + 3];
				UIVertex vert5 = stream[i + 4];
				UIVertex vert6 = stream[i + 5];

				// Get the "width" and "height" of the letter
				float xOffset = Mathf.Abs(vert1.position.x - vert2.position.x) / 2f;
				float yOffset = Mathf.Abs(vert1.position.y - vert3.position.y) / 2f;

				Vector2 letterCenter = radius * (Quaternion.Euler(0, 0, ((float)i / 6f) * rotationAngle) * new Vector2(0f, 1f));

				vert1.position = new Vector3(letterCenter.x - xOffset, letterCenter.y + yOffset, vert1.position.z);
				vert2.position = new Vector3(letterCenter.x + xOffset, letterCenter.y + yOffset, vert2.position.z);
				vert3.position = new Vector3(letterCenter.x + xOffset, letterCenter.y - yOffset, vert3.position.z);
				vert4.position = new Vector3(letterCenter.x + xOffset, letterCenter.y - yOffset, vert4.position.z);
				vert5.position = new Vector3(letterCenter.x - xOffset, letterCenter.y - yOffset, vert5.position.z);
				vert6.position = new Vector3(letterCenter.x - xOffset, letterCenter.y + yOffset, vert6.position.z);

				newStream.Add(vert1);
				newStream.Add(vert2);
				newStream.Add(vert3);
				newStream.Add(vert4);
				newStream.Add(vert5);
				newStream.Add(vert6);
			}

			toFill.AddUIVertexTriangleStream(newStream);
		}

	    #endregion
	}
}
