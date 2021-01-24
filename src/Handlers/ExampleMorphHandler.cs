using System.Collections.Generic;

namespace everlaster
{
    class ExampleMorphHandler
    {
        private Dictionary<string, List<BasicMorphConfig>> examples;
        public Dictionary<string, string> ExampleNames { get; set; }

        public ExampleMorphHandler()
        {
            ExampleNames = new Dictionary<string, string>()
            {
                { "bigNaturals", "Pornstar big naturals" },
                { "smallAndPerky", "Small and perky" },
                { "mediumImplants", "Medium implants" },
                { "hugeAndSoft", "Huge and soft" },
            };

            examples = new Dictionary<string, List<BasicMorphConfig>>();
            examples.Add("bigNaturals", new List<BasicMorphConfig>
            {
                //                      morph                       value
                new BasicMorphConfig("Breast Height Upper",       0.175f),
                new BasicMorphConfig("Breast Pointed",            0.100f),
                new BasicMorphConfig("Breast Round",             -0.500f),
                new BasicMorphConfig("Breast Top Curve2",        -0.175f),
                new BasicMorphConfig("Breast Zero",               0.100f),
                new BasicMorphConfig("Breasts Natural Left",     -0.150f),
                new BasicMorphConfig("Breasts Natural Right",     0.075f),
                new BasicMorphConfig("Breasts Size",              0.050f),
                new BasicMorphConfig("BreastsShape2",            -0.175f),
                new BasicMorphConfig("Nipple Diameter",          -0.400f),
                new BasicMorphConfig("Nipple Size",              -0.400f),
                new BasicMorphConfig("Nipple Length",            -0.200f),
            });
            examples.Add("smallAndPerky", new List<BasicMorphConfig>
            {
                //                      morph                       value
                new BasicMorphConfig("Areolae Perk",              0.400f),
                new BasicMorphConfig("Areola Size",              -0.300f),
                new BasicMorphConfig("Breast diameter",          -0.050f),
                new BasicMorphConfig("Breast Sag1",               0.100f),
                new BasicMorphConfig("Breast Sag2",               0.150f),
                new BasicMorphConfig("Breast Pointed",            0.300f),
                new BasicMorphConfig("Breast Round",             -0.300f),
                new BasicMorphConfig("Breasts Cleavage",          0.150f),
                new BasicMorphConfig("Breasts Natural Left",      0.150f),
                new BasicMorphConfig("Breasts Natural Right",     0.190f),
                new BasicMorphConfig("Breasts Implants Left",    -0.025f),
                new BasicMorphConfig("Breasts Implants Right",   -0.025f),
                new BasicMorphConfig("Breasts Small",             0.150f),
                new BasicMorphConfig("Breasts Under Curve",       0.150f),
                new BasicMorphConfig("Nipple Diameter",          -0.600f),
                new BasicMorphConfig("Nipple Length",            -0.300f),
                new BasicMorphConfig("Nipple Size",              -0.100f),
                new BasicMorphConfig("Sternum Width",             0.100f),
            });
            examples.Add("mediumImplants", new List<BasicMorphConfig>
            {
                //                      morph                       value
                new BasicMorphConfig("Breast diameter",           0.500f),
                new BasicMorphConfig("Breast Round",             -0.250f),
                new BasicMorphConfig("Breast Under Smoother3",   -0.350f),
                new BasicMorphConfig("Breasts Cleavage",          0.500f),
                new BasicMorphConfig("Breasts Implants Left",     0.200f),
                new BasicMorphConfig("Breasts Implants Right",    0.180f),
                new BasicMorphConfig("Breasts Natural Left",     -0.150f),
                new BasicMorphConfig("Breasts Natural Right",    -0.125f),
                new BasicMorphConfig("Breasts Perk Side",         0.300f),
                new BasicMorphConfig("Breasts Size",             -0.350f),
                new BasicMorphConfig("Chest Smoother",           -0.500f),
                new BasicMorphConfig("Nipple Size",              -0.500f),
                new BasicMorphConfig("Nipple Length",            -0.350f),
                new BasicMorphConfig("Nipples Size",              0.150f),
                new BasicMorphConfig("Sternum Width",             0.750f),
            });
            examples.Add("hugeAndSoft", new List<BasicMorphConfig>
            {
                //                      morph                       value
                new BasicMorphConfig("Areola Size",               0.500f),
                new BasicMorphConfig("Areola Size X",            -0.250f),
                new BasicMorphConfig("Areola Size Y",             0.650f),
                new BasicMorphConfig("Areola Puffy Edge",         0.500f),
                new BasicMorphConfig("Areolae Diameter",          0.250f),
                new BasicMorphConfig("Areolae Perk",              0.500f),
                new BasicMorphConfig("Breasts Cleavage",         -0.100f),
                new BasicMorphConfig("Breasts Gone",             -0.100f),
                new BasicMorphConfig("Breasts Perk Side",         0.450f),
                new BasicMorphConfig("Breasts Size",             -0.100f),
                new BasicMorphConfig("BreastsShape3",            -0.300f),
                new BasicMorphConfig("Nipple Diameter",          -0.333f),
                new BasicMorphConfig("Nipples Large",            -0.100f),
                new BasicMorphConfig("ChestUnderBreast",          0.250f),
                new BasicMorphConfig("Sternum Width",             0.250f),
            });
        }

        public void Update(string example)
        {
            examples[example].ForEach(it => it.UpdateVal());
        }

        public void ResetMorphs()
        {
            examples["bigNaturals"].ForEach(it => it.Reset());
            examples["smallAndPerky"].ForEach(it => it.Reset());
            examples["mediumImplants"].ForEach(it => it.Reset());
            examples["hugeAndSoft"].ForEach(it => it.Reset());
        }

        public string GetStatus(string example)
        {
            string text = $"> {ExampleNames[example]} morph tweaks:\n";
            examples[example].ForEach(it =>
            {
                text = text + Formatting.NameValueString(it.Name, it.Morph.morphValue) + "\n";
            });
            return text;
        }
    }
}
