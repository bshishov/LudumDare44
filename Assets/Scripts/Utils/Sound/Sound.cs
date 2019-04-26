using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Assets.Scripts.Utils.Sound
{
    [Serializable]
    public class Sound
    {
        public AudioClip Clip;
        [Range(0f, 1.5f)] public float VolumeModifier = 1f;
    
        public AudioMixerGroup MixerGroup;
        public bool Loop = false;
        public bool IgnoreListenerPause = false;

        [Header("Pitch")]
        [Range(0.5f, 2f)] public float Pitch = 1f;
        public bool RandomizePitch = false;
        [Range(0f, 0.2f)]
        public float MaxPitchShift = 0.05f;

        [Header("Delay")]
        [Range(0, 1f)] public float Delay = 0f;
        public bool RandomizeDelay = false;
        [Range(0, 2f)] public float MaxAdditionalDelay = 0;
    }
}