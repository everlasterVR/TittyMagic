using System.Collections.Generic;

public static class ListExtension
{
    public static List<JSONStorable> Prune(this List<JSONStorable> list)
    {
        list.RemoveAll(storable => storable == null || storable.containingAtom == null);
        return list;
    }
}
