using System.Collections.Generic;

namespace TittyMagic
{
    public static class SoftColliderGroup
    {
        public static IEnumerable<string> allGroups => new List<string> { MAIN, OUTER, AREOLA, NIPPLE };
        public const string MAIN = "Main";
        public const string OUTER = "Outer";
        public const string AREOLA = "Areola";
        public const string NIPPLE = "Nipple";
    }
}
