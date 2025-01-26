using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using dotmob;

#if DM_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace WordCross
{
	public class StorePopup : Popup
	{
		#region Member Variables

		private bool areAdsRemoved;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			IAPManager.Instance.OnProductPurchased += OnProductPurchases;
		}

		public override void OnHiding()
		{
			base.OnHiding();

			IAPManager.Instance.OnProductPurchased -= OnProductPurchases;
		}

		#endregion

		#region Private Methods

		private void OnProductPurchases(string productId)
		{
			Hide(false);

			PopupManager.Instance.Show("product_purchased");
		}

		#endregion
	}
}
