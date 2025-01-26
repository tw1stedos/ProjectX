using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WordCross
{
	[CustomEditor(typeof(GameController))]
	public class GameControllerEditor : Editor
	{
		#region Enums

		private enum ListAction
		{
			None,
			MoveUp,
			MoveDown,
			Remove
		}

		#endregion

		#region Delegates

		delegate ListAction DrawListProperty(SerializedProperty listProperty, int index, List<int> parentIndices);
		delegate void MoveListElements(int fromIndex, int toIndex, List<int> parentIndices);

		#endregion

		#region Public Methods

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();

			DrawList(serializedObject.FindProperty("packInfos"), DrawPackInfo, MovePackInfoItem, null);

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Private Methods

		private ListAction DrawPackInfo(SerializedProperty packInfoProperty, int index, List<int> parentIndices)
		{
			ListAction listAction = DrawRemovableFoldout(packInfoProperty);

			if (packInfoProperty.isExpanded)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.PropertyField(packInfoProperty.FindPropertyRelative("packName"));
				EditorGUILayout.PropertyField(packInfoProperty.FindPropertyRelative("color"));
				EditorGUILayout.PropertyField(packInfoProperty.FindPropertyRelative("background"));

				DrawList(packInfoProperty.FindPropertyRelative("categoryInfos"), DrawCategoryInfo, MoveCategoryInfoItem, new List<int> () { index });

				EditorGUI.indentLevel--;
			}

			return listAction;
		}

		private ListAction DrawCategoryInfo(SerializedProperty categoryInfoProperty, int index, List<int> parentIndices)
		{
			ListAction listAction = DrawRemovableFoldout(categoryInfoProperty);

			if (categoryInfoProperty.isExpanded)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.PropertyField(categoryInfoProperty.FindPropertyRelative("displayName"));
				EditorGUILayout.PropertyField(categoryInfoProperty.FindPropertyRelative("coinsAwarded"));

				List<int> newParentIndices = new List<int>(parentIndices);

				newParentIndices.Add(index);

				EditorGUILayout.BeginHorizontal();

				GUILayout.Space(60f);

				if (GUILayout.Button("Add Selected Level Files"))
				{
					List<PackInfo>		packInfos		= (target as GameController).PackInfos;
					List<CategoryInfo>	categoryInfos	= packInfos[parentIndices[0]].categoryInfos;
					CategoryInfo		categoryInfo	= categoryInfos[index];

					AddSelectedObjectsToLevelFiles(categoryInfo);
				}

				EditorGUILayout.EndHorizontal();

				DrawList(categoryInfoProperty.FindPropertyRelative("levelFiles"), DrawLevelFile, MoveLevelFileItem, newParentIndices);

				EditorGUI.indentLevel--;
			}

			return listAction;
		}

		private ListAction DrawLevelFile(SerializedProperty levelInfoProperty, int index, List<int> parentIndices)
		{
			return DrawRemovableProperty(levelInfoProperty);
		}

		private void MovePackInfoItem(int fromIndex, int toIndex, List<int> parentIndices)
		{
			List<PackInfo>	packInfos		= (target as GameController).PackInfos;
			PackInfo		packItemToMove	= packInfos[fromIndex];

			packInfos.RemoveAt(fromIndex);
			packInfos.Insert(toIndex, packItemToMove);
		}

		private void MoveCategoryInfoItem(int fromIndex, int toIndex, List<int> parentIndices)
		{
			List<PackInfo>		packInfos		= (target as GameController).PackInfos;
			List<CategoryInfo>	categoryInfos	= packInfos[parentIndices[0]].categoryInfos;
			CategoryInfo		categoryInfo	= categoryInfos[fromIndex];

			categoryInfos.RemoveAt(fromIndex);
			categoryInfos.Insert(toIndex, categoryInfo);
		}

		private void MoveLevelFileItem(int fromIndex, int toIndex, List<int> parentIndices)
		{
			List<PackInfo>		packInfos		= (target as GameController).PackInfos;
			List<CategoryInfo>	categoryInfos	= packInfos[parentIndices[0]].categoryInfos;
			List<TextAsset>		levelFiles		= categoryInfos[parentIndices[1]].levelFiles;
			TextAsset			levelFile		= levelFiles[fromIndex];

			levelFiles.RemoveAt(fromIndex);
			levelFiles.Insert(toIndex, levelFile);
		}

		private void DrawList(SerializedProperty listProperty, DrawListProperty drawListPropertyCallback, MoveListElements moveListElementsCallback, List<int> parentIndices)
		{
			listProperty.isExpanded = EditorGUILayout.Foldout(listProperty.isExpanded, listProperty.displayName);

			if (listProperty.isExpanded)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.PropertyField(listProperty.FindPropertyRelative("Array.size"));

				for (int i = 0; i < listProperty.arraySize; i++)
				{
					SerializedProperty listItemProperty = listProperty.GetArrayElementAtIndex(i);

					ListAction listAction = drawListPropertyCallback(listItemProperty, i, parentIndices);

					switch (listAction)
					{
						case ListAction.MoveUp:
							if (i > 0)
							{
								Undo.RecordObject(target, "Move Element Up");
								moveListElementsCallback(i, i - 1, parentIndices);
							}
							break;
						case ListAction.MoveDown:
							if (i < listProperty.arraySize - 1)
							{
								Undo.RecordObject(target, "Move Element Down");
								moveListElementsCallback(i, i + 1, parentIndices);
							}
							break;
						case ListAction.Remove:
							Undo.RecordObject(target, "Delete Element");
							listProperty.DeleteArrayElementAtIndex(i);
							break;
					}
				}

				EditorGUI.indentLevel--;
			}
		}

		private ListAction DrawRemovableFoldout(SerializedProperty property)
		{
			ListAction action = ListAction.None;

			EditorGUILayout.BeginHorizontal();

			property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, property.displayName);

			if (GUILayout.Button("^", GUILayout.Width(30f)))
			{
				action = ListAction.MoveUp;
			}

			if (GUILayout.Button("v", GUILayout.Width(30f)))
			{
				action = ListAction.MoveDown;
			}

			if (GUILayout.Button("Remove", GUILayout.Width(70f)))
			{
				action = ListAction.Remove;
			}

			EditorGUILayout.EndHorizontal();

			return action;
		}

		private ListAction DrawRemovableProperty(SerializedProperty property)
		{
			ListAction action = ListAction.None;

			EditorGUILayout.BeginHorizontal();

			property.isExpanded = EditorGUILayout.PropertyField(property);

			if (GUILayout.Button("^", GUILayout.Width(30f)))
			{
				action = ListAction.MoveUp;
			}

			if (GUILayout.Button("v", GUILayout.Width(30f)))
			{
				action = ListAction.MoveDown;
			}

			if (GUILayout.Button("Remove", GUILayout.Width(70f)))
			{
				action = ListAction.Remove;
			}

			EditorGUILayout.EndHorizontal();

			return action;
		}

		private void AddSelectedObjectsToLevelFiles(CategoryInfo categoryInfo)
		{
			List<TextAsset> selectedTextAssets = GetSelectedTextAssets();

			for (int i = 0; i < selectedTextAssets.Count; i++)
			{
				categoryInfo.levelFiles.Add(selectedTextAssets[i]);
			}
		}

		private List<TextAsset> GetSelectedTextAssets()
		{
			Object[]		selectedObjects		= Selection.objects;
			List<TextAsset>	selectedTextAssets	= new List<TextAsset>();

			for (int i = 0; i < selectedObjects.Length; i++)
			{
				TextAsset textAsset = selectedObjects[i] as TextAsset;

				if (textAsset != null)
				{
					selectedTextAssets.Add(textAsset);
				}
			}

			selectedTextAssets.Sort((TextAsset textAsset1, TextAsset textAsset2) =>
			{
				string assetName1 = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(textAsset1));
				string assetName2 = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(textAsset2));

				if (assetName1.Length != assetName2.Length)
				{
					return assetName1.Length - assetName2.Length;
				}

				return assetName1.CompareTo(assetName2);
			});

			return selectedTextAssets;
		}

		#endregion
	}
}
