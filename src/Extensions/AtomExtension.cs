using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class AtomExtension
{
    public static List<JSONStorable> FindStorablesByRegexMatch(this Atom atom, Regex regex) =>
        atom.GetStorableIDs()
            .Where(id => regex.IsMatch(id))
            .Select(atom.GetStorableByID)
            .ToList()
            .Prune();

    public static bool FreezeGrabbing(this Atom atom) => atom.grabFreezePhysics && atom.mainController.isGrabbing;
}
