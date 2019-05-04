using UnityEngine;

namespace Utils
{
    public class PercentageAttribute : PropertyAttribute
    {
        public float Min = 0f;
        public float Max = 1f;
        public readonly bool Relative = false;

        public PercentageAttribute(float min, float max, bool relative=false)
        {
            Min = min;
            Max = max;
            Relative = relative;
        }

        public string ValueToString(float val)
        {
            if (Relative)
            {
                if (val > 0)
                    return $"+{val * 100}";

                if (val < 0)
                    return $"-{val * 100}";
            }

            return $"{val * 100}";
        }
    }
}
