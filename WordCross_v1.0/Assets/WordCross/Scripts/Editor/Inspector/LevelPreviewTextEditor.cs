using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace WordCross
{
	[CustomEditor(typeof(LevelPreviewText))]
	public class LevelPreviewTextEditor : UnityEditor.UI.TextEditor
	{
		#region Public Methods

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			SerializedProperty textProperty		= serializedObject.FindProperty("m_Text");
			SerializedProperty fontDataProperty	= serializedObject.FindProperty("m_FontData");

			textProperty.stringValue = EditorGUILayout.TextField("Letters", textProperty.stringValue);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"));
			EditorGUILayout.PropertyField(fontDataProperty.FindPropertyRelative("m_Font"));
			EditorGUILayout.PropertyField(fontDataProperty.FindPropertyRelative("m_FontStyle"));
			EditorGUILayout.PropertyField(fontDataProperty.FindPropertyRelative("m_FontSize"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));

			EditorGUILayout.Space();

			Text targetText = target as Text;

			targetText.horizontalOverflow	= HorizontalWrapMode.Overflow;
			targetText.verticalOverflow		= VerticalWrapMode.Overflow;

			serializedObject.ApplyModifiedProperties();
		}

		#endregion
	}
}
