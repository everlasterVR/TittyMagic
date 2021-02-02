using System.Collections.Generic;

namespace everlaster
{
    class NippleErectionMorphHandler
    {
        private List<BasicMorphConfig> morphs;

        public NippleErectionMorphHandler()
        {
            morphs = new List<BasicMorphConfig>
            {
                //                             morph           base
                new BasicMorphConfig("TM_NippleErection",      1.000f),
            };
        }

        public void Update(float nippleErection)
        {
            morphs.ForEach(it => it.UpdateVal(nippleErection));
        }

        public void ResetAll()
        {
            morphs.ForEach(it => it.Reset());
        }
    }
}
