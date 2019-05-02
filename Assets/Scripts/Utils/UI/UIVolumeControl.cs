using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class UIVolumeControl : MonoBehaviour
    {
        public AudioMixer AudioMixer;
        public const float Scale = 20f;

        public Slider SoundSlider;
        public Slider MusicSlider;

        void Awake()
        {
            if (AudioMixer != null)
            {
                SetSoundVolumeLinear(PlayerPrefs.GetFloat(Common.SoundParameters.SoundVolume, 1f));
                SetMusicVolumeLinear(PlayerPrefs.GetFloat(Common.SoundParameters.MusicVolume, 1f));
            }
        }

        void Start()
        {
            if (SoundSlider != null)
                SoundSlider.value = PlayerPrefs.GetFloat(Common.SoundParameters.SoundVolume, 1f);

            if (MusicSlider != null)
                MusicSlider.value = PlayerPrefs.GetFloat(Common.SoundParameters.MusicVolume, 1f);
        }

        public void SetVolumeLinear(string volumeParam, float volume, bool save = true)
        {
            if (AudioMixer == null)
                return;

            var level = Mathf.Max(0.01f, Mathf.Clamp01(volume));
            if (save)
                PlayerPrefs.SetFloat(volumeParam, level);

            AudioMixer.SetFloat(volumeParam, Mathf.Log(level) * Scale);
        }

        public void SetMusicVolumeLinear(float volume)
        {
            SetVolumeLinear(Common.SoundParameters.MusicVolume, volume);
        }

        public void SetSoundVolumeLinear(float volume)
        {
            SetVolumeLinear(Common.SoundParameters.SoundVolume, volume);
        }
    }
}
