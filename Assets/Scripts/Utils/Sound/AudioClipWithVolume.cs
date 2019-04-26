using System;
using UnityEngine;

namespace Assets.Scripts.Utils.Sound
{
    [Serializable]
    public class AudioClipWithVolume
    {
        public AudioClip Clip;

        [Range(0f, 1.5f)]
        public float VolumeModifier = 1f;

        public float Pitch = 1f;
    }
}