using System;
using System.Collections.Generic;
using System.Linq;

public static class HelperMethods
{
    // Fisher-Yates Shuffle
    public static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Algorithm I found to get combinations
    public static IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1)
        {
            return list.Select(item => new T[] { item });
        }

        return list.SelectMany((item, index) => GetCombinations(list.Skip(index + 1), length - 1).Select(c => new T[] { item }.Concat(c)));
    }


    public static int CompareValues(List<int> values1, List<int> values2)
    {
        int count = Math.Min(values1.Count, values2.Count);

        for (int i = 0; i < count; i++)
        {
            if (values1[i] > values2[i])
            {
                return 1;
            }

            if (values1[i] < values2[i])
            {
                return -1;
            }
        }

        return 0;
    }
}
