using System.Collections.Generic;

namespace everlaster
{
    class NippleErectionMorphHandler
    {
        private List<NippleErectionMorphConfig> morphs;

        public NippleErectionMorphHandler()
        {
            morphs = new List<NippleErectionMorphConfig>
            {
                //                             morph                       base       start
                new NippleErectionMorphConfig("TM_Natural Nipples",       -0.100f,    0.025f), // Spacedog.Import_Reloaded_Lite.2
                new NippleErectionMorphConfig("TM_Nipple",                 0.500f,   -0.125f), // Spacedog.Import_Reloaded_Lite.2
                new NippleErectionMorphConfig("TM_Nipple Length",         -0.200f,    0.050f),
                new NippleErectionMorphConfig("TM_Nipples Apply",          0.500f,   -0.125f),
                new NippleErectionMorphConfig("TM_Nipples Bulbous",        0.600f,   -0.150f), // kemenate.Morphs.10
                new NippleErectionMorphConfig("TM_Nipples Large",          0.300f,   -0.075f),
                new NippleErectionMorphConfig("TM_Nipples Sag",           -0.200f,    0.050f), // kemenate.Morphs.10
                new NippleErectionMorphConfig("TM_Nipples Tilt",           0.200f,   -0.050f), // kemenate.Morphs.10
            };
        }

        public void Update(float nippleErection)
        {
            morphs.ForEach(it => it.UpdateVal(nippleErection));
        }

        public void ResetMorphs(string type = "")
        {
            morphs.ForEach(it => it.Reset());
        }
    }
}
