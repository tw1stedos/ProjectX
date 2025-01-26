using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WordCross
{
	[System.Serializable]
	public class CategoryInfo
	{
		#region Inspector Variables
		
		[Tooltip("The category name to display in the game")]
		public string displayName = "";

		[Tooltip("The amount of coins to award once all levels in the category have been completed")]
		public int coinsAwarded = 0;

		[Tooltip("The list of level text files for this category")]
		public List<TextAsset> levelFiles = null;

		#endregion

		#region Properties

		/// <summary>
		/// List of all the LevelDatas for each level file
		/// </summary>
		public List<LevelData> LevelDatas { get; set; }

		#endregion
	}
}
