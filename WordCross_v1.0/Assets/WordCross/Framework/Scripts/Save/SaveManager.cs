using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace dotmob
{
	public class SaveManager : SingletonComponent<SaveManager>
	{
		#region Member Variables

		private List<ISaveable>	saveables;
		private JSONNode		loadedSave;

		#endregion

		#region Properties

		/// <summary>
		/// Path to the save file on the device
		/// </summary>
		public string SaveFilePath { get { return Application.persistentDataPath + "/save.json"; } }

		/// <summary>
		/// List of registered saveables
		/// </summary>
		private List<ISaveable> Saveables
		{
			get
			{
				if (saveables == null)
				{
					saveables = new List<ISaveable>();
				}

				return saveables;
			}
		}

		#endregion

		#if UNITY_EDITOR

		[UnityEditor.MenuItem("Dotmob/Delete Save Data")]
		public static void DeleteSaveData()
		{
			if (!System.IO.File.Exists(SaveManager.Instance.SaveFilePath))
			{
				UnityEditor.EditorUtility.DisplayDialog("Delete Save File", "There is no save file.", "Ok");

				return;
			}

			bool delete = UnityEditor.EditorUtility.DisplayDialog("Delete Save File", "Delete the save file located at " + SaveManager.Instance.SaveFilePath, "Yes", "No");

			if (delete)
			{
				System.IO.File.Delete(SaveManager.Instance.SaveFilePath);

				UnityEditor.EditorUtility.DisplayDialog("Delete Save File", "Save file has been deleted.", "Ok");
			}
		}

		[UnityEditor.MenuItem("Window/Print Save Data To Console")] 
		public static void PrintSaveFileToConsole()
		{
			if (!System.IO.File.Exists(SaveManager.Instance.SaveFilePath))
			{
				UnityEditor.EditorUtility.DisplayDialog("Delete Save File", "There is no save file.", "Ok");

				return;
			}

			string contents = System.IO.File.ReadAllText(SaveManager.Instance.SaveFilePath);

			Debug.Log(contents);
		}

		#endif

		#region Unity Methods

		private void Start()
		{
			Debug.Log("Save file path: " + SaveFilePath);
		}

		private void OnDestroy()
		{
			Save();
		}

		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				Save();
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Registers a saveable to be saved
		/// </summary>
		public void Register(ISaveable saveable)
		{
			Saveables.Add(saveable);
		}

		/// <summary>
		/// Loads the save data for the given saveable
		/// </summary>
		public JSONNode LoadSave(ISaveable saveable)
		{
			return LoadSave(saveable.SaveId);
		}

		/// <summary>
		/// Loads the save data for the given save id
		/// </summary>
		public JSONNode LoadSave(string saveId)
		{
			// Check if the save file has been loaded and if not try and load it
			if (loadedSave == null && !LoadSave(out loadedSave))
			{
				return null;
			}

			// Check if the loaded save file has the given save id
			if (!loadedSave.AsObject.HasKey(saveId))
			{
				return null;
			}

			// Return the JSONNode for the save id
			return loadedSave[saveId];
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Saves all registered saveables to the save file
		/// </summary>
		private void Save()
		{
			Dictionary<string, object> saveJson = new Dictionary<string, object>();

			for (int i = 0; i < saveables.Count; i++)
			{
				saveJson.Add(saveables[i].SaveId, saveables[i].Save());
			}

			System.IO.File.WriteAllText(SaveFilePath, Utilities.ConvertToJsonString(saveJson));
		}

		/// <summary>
		/// Tries to load the save file
		/// </summary>
		private bool LoadSave(out JSONNode json)
		{
			json = null;

			if (!System.IO.File.Exists(SaveFilePath))
			{
				return false;
			}

			json = JSON.Parse(System.IO.File.ReadAllText(SaveFilePath));

			return json != null;
		}

		#endregion
	}
}
