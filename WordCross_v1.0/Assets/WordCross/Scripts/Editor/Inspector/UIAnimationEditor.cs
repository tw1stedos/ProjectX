using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WordCross
{
	[CustomEditor(typeof(UIAnimation))]
	public class UIAnimationEditor : Editor
	{
		#region Member Variables

		private static GUIContent fromContent = new GUIContent("From");
		private static GUIContent toContent = new GUIContent("To");

		#endregion

		#region Properties

		#endregion

		#region Unity Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("style"));
			
			if (serializedObject.FindProperty("style").enumValueIndex == (int)UIAnimation.Style.Custom)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("animationCurve"));
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("startDelay"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("startOnFirstFrame"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("loopType"));

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("useCurrentFrom"));

			switch ((UIAnimation.Type)serializedObject.FindProperty("type").enumValueIndex)
			{
				case UIAnimation.Type.PositionX:
				case UIAnimation.Type.PositionY:
				case UIAnimation.Type.ScaleX:
				case UIAnimation.Type.ScaleY: 
				case UIAnimation.Type.RotationZ: 
				case UIAnimation.Type.Width:
				case UIAnimation.Type.Height:
				case UIAnimation.Type.Alpha:
					DrawFloatValues();
					break;
				case UIAnimation.Type.Color:
					DrawColorValues();
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Private Methods

		private void DrawFloatValues()
		{
			bool useCurrentFrom = serializedObject.FindProperty("useCurrentFrom").boolValue;

			GUI.enabled = !useCurrentFrom;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("fromValue"), fromContent);

			GUI.enabled = true;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("toValue"), toContent);
		}

		private void DrawColorValues()
		{
			bool useCurrentFrom = serializedObject.FindProperty("useCurrentFrom").boolValue;

			GUI.enabled = !useCurrentFrom;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("fromColor"), fromContent);

			GUI.enabled = true;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("toColor"), toContent);
		}

		#endregion
	}
}
