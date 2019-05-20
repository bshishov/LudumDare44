using System;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class StackableProperty
    {
        [Serializable]
        public enum ModifierType
        {
            Addition,
            Percentage
        }

        public ModifierType Type = ModifierType.Addition;
        public float BaseValue;
        public float ModifierPerStack;
        public float EffectiveStacks = 1;

        public float GetValue(float stacks = 1f)
        {
            // DO NOT CHANGE THIS! unless you do not know what you are doing!
            var stackedMod = ModifierPerStack * stacks / ((stacks - 1) * (1 - 0.3f) / (Mathf.Max(1, EffectiveStacks)) + 1);

            switch (Type)
            {
                case ModifierType.Addition:
                    return BaseValue + stackedMod;
                case ModifierType.Percentage:
                    return BaseValue * (1 + ELU(stackedMod));
                default:
                    return BaseValue + stackedMod;
            }
        }

        /// <summary>
        /// Exponential linear unit. Used for multiplier modifiers to not go pass the -1 on the left side
        /// </summary>
        /// <param name="x"></param>
        /// <param name="alpha"></param>
        /// <returns>Returns the value in range (-1, +inf) </returns>
        public static float ELU(float x, float alpha = 1f)
        {
            if (x >= 0)
                return x;
            return alpha * (Mathf.Exp(x) - 1);
        }
    }
}
