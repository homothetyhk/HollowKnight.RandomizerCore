using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore
{
    internal static class Extensions
    {
        public static void Log<T>(this IEnumerable<T> list)
        {
            foreach (T item in list)
            {
                Logger.LogFine($"\n    {item}");
            }
        }

        public static void Log<T1, T2>(this Dictionary<T1, T2> dict)
        {
            foreach (var kvp in dict)
            {
                Logger.LogFine($"\n{kvp.Key} : {kvp.Value}");
            }
        }

        public static T Pop<T>(this List<T> list, int index = 0)
        {
            T val = list[index];
            list.RemoveAt(index);
            return val;
        }

        public static T Pop<T>(this List<T> list, Predicate<T> TSelector)
        {
            int i = list.FindIndex(TSelector);
            return list.Pop(i);
        }

        public static bool TryPop<T>(this List<T> list, Predicate<T> TSelector, out T val)
        {
            int i = list.FindIndex(TSelector);
            if (i < 0)
            {
                val = default;
                return false;
            }
            else
            {
                val = list.Pop(i);
                return true;
            }
        }

        public static T Next<T>(this Random rand, IList<T> ts)
        {
            return ts[rand.Next(ts.Count())];
        }

        public static int[] Permute(this Random rand, int n)
        {
            int[] arr = new int[n];
            for (int i = 0; i < n; i++) arr[i] = i;
            rand.PermuteInPlace(arr);
            return arr;
        }

        public static T[] Permute<T>(this Random rand, T[] input)
        {
            T[] output = input.Clone() as T[];
            rand.PermuteInPlace(output);
            return output;
        }

        private static void PermuteInPlace<T>(this Random rand, T[] input)
        {
            for (int i = input.Length - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                T temp = input[i];
                input[i] = input[j];
                input[j] = temp;
            }
        }

        public static void Swap<T>(this T[] arr, int i, int j)
        {
            T t = arr[i];
            arr[i] = arr[j];
            arr[j] = t;
        }
    }
}
