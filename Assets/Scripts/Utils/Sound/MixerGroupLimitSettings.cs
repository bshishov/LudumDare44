using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Assets.Scripts.Utils.Sound
{
    [CreateAssetMenu(fileName = "MixerGroupLimitSettings", menuName = "Audio/Mixer Group Limit Settings")]
    public class MixerGroupLimitSettings : ScriptableObject
    {
        [Serializable]
        public class MixerGroupLimitSettingsEntry
        {
            public AudioMixerGroup MixerGroup;
            public int Limit = 10;
        }

        public MixerGroupLimitSettingsEntry[] Settings;
        public int DefaultLimit = 0;

        private Dictionary<AudioMixerGroup, int> _limits = null;

        public int GetLimit(AudioMixerGroup mixerGroup)
        {
            // LazyLoading limits
            if (_limits == null)
            {
                _limits = new Dictionary<AudioMixerGroup, int>();
                foreach (var entry in Settings)
                {
                    _limits.Add(entry.MixerGroup, entry.Limit);
                }
            }

            int limit;
            if (_limits.TryGetValue(mixerGroup, out limit))
            {
                return limit;
            }
            return DefaultLimit;
        }
    }
}