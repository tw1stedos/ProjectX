using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace dotmob
{
	public class MobileAdsManager : MonoBehaviour
	{
	

		private void Awake()
		{
			
		}

		private void Start()
		{
			Advertisements.Instance.Initialize();

			Advertisements.Instance.ShowBanner(BannerPosition.BOTTOM, BannerType.Banner);

			
		}

        

        ///// <summary>
        ///// Removes ads for this user
        ///// </summary>
        public void RemoveAds()
        {

            Advertisements.Instance.RemoveAds(true);
            
        }

        
    } 
}
