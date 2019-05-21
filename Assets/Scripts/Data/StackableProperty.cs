using System;
using Utils;

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
            var stackedMod = MathUtils.StackedModifier(ModifierPerStack, stacks, EffectiveStacks);

            switch (Type)
            {
                case ModifierType.Addition:
                    return BaseValue + stackedMod;
                case ModifierType.Percentage:
                    return BaseValue * (1 + MathUtils.ELU(stackedMod));
                default:
                    return BaseValue + stackedMod;
            }
        }
    }
}
