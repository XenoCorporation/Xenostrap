using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenostrap.Utility
{
    internal class FixedSizeList<T>(int size) : List<T>
    {
        public int MaxSize { get; } = size;

        public new void Add(T item)
        {
            if (Count >= MaxSize)
                RemoveAt(Count - 1);
            base.Add(item);
        }
    }
}
