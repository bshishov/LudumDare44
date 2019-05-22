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
            Exact = 0,
            Add = 1,
            Mult = 2
        }

        public ModifierType Type = ModifierType.Exact;
        public float BaseValue;
        public float ModifierPerStack;
        public float EffectiveStacks = 1;

        public float GetValue(float stacks = 1f)
        {
            var stackedMod = MathUtils.StackedModifier(ModifierPerStack, stacks, EffectiveStacks);

            switch (Type)
            {
                case ModifierType.Add:
                    return BaseValue + stackedMod;
                case ModifierType.Mult:
                    return BaseValue * (1 + MathUtils.ELU(stackedMod));
                default:
                case ModifierType.Exact:
                    return BaseValue;
            }
        }
    }
}
