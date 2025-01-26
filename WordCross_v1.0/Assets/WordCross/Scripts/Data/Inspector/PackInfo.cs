using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WordCross
{
	[System.Serializable]
	public class PackInfo
	{
		#region Inspector Varaibles

		public string				packName		= "";
		public Color				color			= Color.white;
		public Sprite				background		= null;
		public List<CategoryInfo>	categoryInfos	= null;

		#endregion
	}
}
