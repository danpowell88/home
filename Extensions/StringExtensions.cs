using System;
public static class StringExtensions
{
    public static string RemoveNonAlphaCharacters(this string str)
    {
        var arr = str.ToCharArray();

        arr = Array.FindAll(arr, (c => (char.IsLetter(c))));
        return new string(arr);
    }
}