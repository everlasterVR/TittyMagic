using System.Collections.Generic;

namespace everlaster
{
    class SizeMorphHandler
    {
        private List<SizeMorphConfig> morphs;

        public SizeMorphHandler()
        {
            morphs = new List<SizeMorphConfig>
            {
                //               morph                           base        start
                new SizeMorphConfig("TM_Baseline",               1.000f),
                //new MorphConfig("Armpit Curve",             -0.100f),
                //new MorphConfig("Breast Diameter",           0.250f),
                //new MorphConfig("Breast Centered",           0.150f),
                //new MorphConfig("Breast Height",             0.250f),
                //new MorphConfig("Breast Large",              0.350f),
                //new MorphConfig("Breasts Natural",          -0.050f),
                //new MorphConfig("BreastsCrease",            -0.250f),
                //new MorphConfig("BreastsShape1",             0.150f),
                //new MorphConfig("BreastsShape2",            -0.050f),
                //new MorphConfig("ChestSeparateBreasts",     -0.025f),

                new SizeMorphConfig("TM_Baseline_Smaller",      -0.333f,     1.000f),
                //new MorphConfig("Breast Small",             -0.140f,     0.420f),

                new SizeMorphConfig("TM_Baseline_Fixer",         0.000f,     1.000f),
                //new MorphConfig("Breast Top Curve1",         0.033f,    -0.033f),
                //new MorphConfig("Breast Top Curve2",         0.250f,    -0.750f),
                //new MorphConfig("Breasts Implants",          0.150f,     0.075f),
                //new MorphConfig("Breasts Size",              0.050f,    -0.050f),
            };
        }

        public void Update(float scale)
        {
            morphs.ForEach(it => it.UpdateVal(scale));
        }

        public string GetStatus()
        {
            string text = "";
            morphs.ForEach((it) =>
            {
                text = text + Formatting.NameValueString(it.Name, it.Morph.morphValue, 1000f, 30) + "\n";
            });
            return text;
        }
    }
}
