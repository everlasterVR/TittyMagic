using System.Collections.Generic;

namespace TittyMagic
{
    internal class NippleErectionMorphHandler
    {
        private HashSet<BasicMorphConfig> morphs;

        public NippleErectionMorphHandler()
        {
            morphs = new HashSet<BasicMorphConfig>
            {
                //                             morph           base
                new BasicMorphConfig("TM_NippleErection",      1.000f),
                //new BasicMorphConfig("Nipples Depth",          0.750f), // Spacedog.Import_Reloaded_Lite.2
                //new BasicMorphConfig("Natural Nipples",       -0.100f), // Spacedog.Import_Reloaded_Lite.2
                //new BasicMorphConfig("Nipple",                 0.500f), // Spacedog.Import_Reloaded_Lite.2
                //new BasicMorphConfig("Nipple Length",         -0.100f),
                //new BasicMorphConfig("Nipples Apply",          0.250f),
                //new BasicMorphConfig("Nipples Bulbous",        0.150f), // kemenate.Morphs.10
                //new BasicMorphConfig("Nipples Sag",           -0.200f), // kemenate.Morphs.10
                //new BasicMorphConfig("Nipples Tilt",           0.200f), // kemenate.Morphs.10
            };
        }

        public void Update(float nippleErection)
        {
            foreach(var it in morphs)
            {
                it.UpdateVal(nippleErection);
            }
        }

        public void ResetAll()
        {
            foreach(var it in morphs)
            {
                it.Reset();
            }
        }
    }
}
