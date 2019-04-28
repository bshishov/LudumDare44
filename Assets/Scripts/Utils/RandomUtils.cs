using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Utils
{
    public static class RandomUtils
    {
        public static T Choice<T>(IList<T> items, Func<T, float> getWeight)
        {
            if (items == null)
                return default(T);
            
            var weights = new float[items.Count];
            var total = 0f;
            for (var i = 0; i < items.Count; i++)
            {
                var w = getWeight(items[i]);
                weights[i] = w;
                total += w;
            }

            var upTo = 0f;
            var r = Random.Range(0, total);

            for (var i = 0; i < items.Count; i++)
            {
                if (upTo + weights[i] >= r)
                {
                    return items[i];
                }

                upTo += weights[i];
            }
            
            return default(T);
        }

        public static T Choice<T>(IList<T> items)
        {
            var idx = Random.Range(0, items.Count);
            return items[idx];
        }
    }
}
