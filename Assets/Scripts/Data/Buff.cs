using Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Data
{
    public enum ModificationParameter
    {
        None = 0,
        HpFlat = 1,  // Think twice while using it
        HpMult = 2,  // Think twice while using it
        MaxHpFlat = 3,
        MaxHpMult = 4,
        DmgFlat,
        DmgMult,
        EvasionChanceFlat,
        SizeFlat,
        SizeMult,
        SpeedFlat,
        SpeedMult,
        SpellStacksFlat,
        CritChanceFlat,
        SpellDamageAmpFlat
    }

    [Serializable]
    public enum BuffStackBehaviour
    {
        MaxStacksOfTwo,
        SumStacks,
        AddNewAsSeparate,
        Discard
    }

    [Serializable]
    public class Modifier
    {
        public ModificationParameter Parameter;
        public float Value;

        // Indicating that the value will not scale too much after this amount of stacks
        public float EffectiveStacks = 10f;
    }

    [Serializable]
    [Flags]
    public enum BuffEventType : int
    {
        OnApply = 1 << 1,
        OnRefresh = 1 << 2,
        OnTick = 1 << 3,
        OnRemove = 1 << 4
    }

    [Serializable]
    public class BuffEvent
    {
        [EnumFlag]
        public BuffEventType EventType;
        
        public Affect Affect;
    }

    [ExecuteInEditMode]
    [CreateAssetMenu(fileName = "Buff", menuName = "Mechanics/Buff")]
    [Serializable]
    public class Buff : ScriptableObject, ISerializationCallbackReceiver
    {
        public string Name;
        public BuffStackBehaviour Behaviour;
        public float TickCooldown = 1f;
        public float Duration = 1f;

        // Changes that will be applied once the buff is applied
        // and will be REVERTED when buff removed
        [Reorderable]
        public Modifier[] Modifiers;
        
        [Reorderable]
        public BuffEvent[] Events;
   
        // Cached events by type for fast access
        public Dictionary<BuffEventType, Affect[]> AffectByEventType => _affectsByEventType;
        [NonSerialized]
        private Dictionary<BuffEventType, Affect[]> _affectsByEventType;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if(_affectsByEventType != null)
                _affectsByEventType.Clear();
            else
                _affectsByEventType = new Dictionary<BuffEventType, Affect[]>();

            if(Events == null || Events.Length == 0)
                return;
            
            foreach (var eventType in Enum.GetValues(typeof(BuffEventType)).Cast<BuffEventType>())
            {
                var affects = Events
                    .Where(e => e.EventType.HasFlag(eventType))
                    .Select(e => e.Affect)
                    .ToArray();
                _affectsByEventType.Add(eventType, affects);
            }
        }
    }
}