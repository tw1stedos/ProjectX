using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace dotmob
{
	[CustomEditor(typeof(IAPProductButton))]
	public class IAPProductButtonEditor : Editor
	{
		#region Inspector Variables

		#endregion

		#region Member Variables

		#endregion

		#region Properties

		#endregion

		#region Unity Methods

		#endregion

		#region Public Methods

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			DrawProductIdsDropdown();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionText"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("priceText"));

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Protected Methods

		#endregion

		#region Private Methods

		private void DrawProductIdsDropdown()
		{
			SerializedProperty productIdProp = serializedObject.FindProperty("productId");

			int				selectedIndex	= 0;
			List<string>	productIds		= new List<string>();

			if (IAPSettings.IsIAPEnabled)
			{
				for (int i = 0; i < IAPSettings.Instance.productInfos.Count; i++)
				{
					string productId = IAPSettings.Instance.productInfos[i].productId;

					if (!string.IsNullOrEmpty(productId))
					{
						productIds.Add(productId);

						if (productIdProp.stringValue == productId)
						{
							selectedIndex = productIds.Count - 1;
						}
					}
				}

				if (productIds.Count == 0)
				{
					EditorGUILayout.HelpBox("There are no product ids set in the IAP Settings. Open the IAP Settings window to add your product ids.", MessageType.Warning);

					if (GUILayout.Button("Open IAP Settings Window"))
					{
						IAPSettingsWindow.Open();
					}

					GUI.enabled = false;

					EditorGUILayout.Popup("Product Id", selectedIndex, productIds.ToArray());
				}
				else
				{
					selectedIndex = EditorGUILayout.Popup("Product Id", selectedIndex, productIds.ToArray());

					productIdProp.stringValue = productIds[selectedIndex];
				}
			}
			else
			{
				EditorGUILayout.HelpBox("IAP is not enabled. Please open the IAP Settings window and enable IAP.", MessageType.Warning);

				if (GUILayout.Button("Open IAP Settings Window"))
				{
					IAPSettingsWindow.Open();
				}

				GUI.enabled = false;

				EditorGUILayout.Popup("Product Id", selectedIndex, productIds.ToArray());
			}

			GUI.enabled = true;
		}

		#endregion
	}
}
