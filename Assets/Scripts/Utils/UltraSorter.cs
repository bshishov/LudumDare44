using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Utils
{
    public class UltraSorter<T>
    {
        public List<T>[] SortedArrays;

        public UltraSorter(int groups)
        {
            SortedArrays = new List<T>[groups];
        }

        public void Add(int group, T item)
        {
            SortedArrays[group].Add(item);
        }

        public void Remove(int group, T item)
        {
            SortedArrays[group].Remove(item);
        }
    }
}
