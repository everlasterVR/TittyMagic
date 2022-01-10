using System;
using System.Linq;

namespace TittyMagic
{
    internal class Utils
    {
        public static MVRScript FindPluginOnAtom(Atom atom, string search)
        {
            var match = atom.GetStorableIDs().FirstOrDefault(s => s.Contains(search));
            return match == null ? null : atom.GetStorableByID(match) as MVRScript;
        }
    }
}
