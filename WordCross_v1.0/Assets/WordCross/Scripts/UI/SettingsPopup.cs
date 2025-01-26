using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using dotmob;

namespace WordCross
{
	public class SettingsPopup : Popup
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private ToggleSlider	musicToggle;
		[SerializeField] private ToggleSlider	soundToggle;
		[SerializeField] private string			privacyPolicyUrl;

		#endregion

		#region Unity Methods

		private void Start()
		{
			musicToggle.SetToggle(SoundManager.Instance.IsMusicOn, false);
			soundToggle.SetToggle(SoundManager.Instance.IsSoundEffectsOn, false);

			musicToggle.OnValueChanged += OnMusicValueChanged;
			soundToggle.OnValueChanged += OnSoundEffectsValueChanged;
		}

		#endregion

		#region Public Methods

		public void OnPrivacyPolicyClicked()
		{
			Application.OpenURL(privacyPolicyUrl);
		}

		#endregion

		#region Private Methods

		private void OnMusicValueChanged(bool isOn)
		{
			SoundManager.Instance.SetSoundTypeOnOff(SoundManager.SoundType.Music, isOn);
		}

		private void OnSoundEffectsValueChanged(bool isOn)
		{
			SoundManager.Instance.SetSoundTypeOnOff(SoundManager.SoundType.SoundEffect, isOn);
		}

		#endregion
	}
}
