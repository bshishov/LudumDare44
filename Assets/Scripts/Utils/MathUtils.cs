using UnityEngine;

namespace Utils
{
    public static class MathUtils
    {
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

        public static float StackedModifier(float modifierValue, float stacks, float effectiveStacks)
        {
            // DO NOT CHANGE THIS! unless you do not know what you are doing!
            return modifierValue * stacks / ((stacks - 1) * (1 - 0.3f) / (Mathf.Max(1, effectiveStacks)) + 1);
        }
    }
}
