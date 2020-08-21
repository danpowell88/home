using System;
using System.Collections.Generic;
using System.Linq;


public static class EnumerableHelper
{
    private static Random r;

    static EnumerableHelper()
    {
        r = new Random();
    }

    public static T Random<T>(IEnumerable<T> input)
    {
        return input.ElementAt(r.Next(input.Count()));
    }
}

public static class EnumerableExtensions
{
    public static T Random<T>(this IEnumerable<T> input)
    {
        return EnumerableHelper.Random(input);
    }
}
