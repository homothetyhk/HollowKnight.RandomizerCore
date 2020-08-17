using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore
{
    public struct Pair<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public Pair(T1 i1, T2 i2)
        {
            Item1 = i1;
            Item2 = i2;
        }
           
        public void Deconstruct(out T1 i1, out T2 i2)
        {
            i1 = Item1;
            i2 = Item2;
        }
    }
}
