using System;

public static class ArrayExtension
{
    public static void Unshift<T>(this T[] array, T value)
    {
        Array.Copy(array, 0, array, 1, array.Length - 1);
        array[0] = value;
    }
}
