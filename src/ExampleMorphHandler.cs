using System.Collections.Generic;

namespace everlaster
{
    class ExampleMorphHandler
    {
        private Dictionary<string, List<ExampleMorphConfig>> examples;
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

            examples = new Dictionary<string, List<ExampleMorphConfig>>();
            examples.Add("bigNaturals", new List<ExampleMorphConfig>
            {
                //                      morph                       value
                new ExampleMorphConfig("Breast Height Upper",       0.175f),
                new ExampleMorphConfig("Breast Pointed",            0.100f),
                new ExampleMorphConfig("Breast Round",             -0.500f),
                new ExampleMorphConfig("Breast Top Curve2",        -0.175f),
                new ExampleMorphConfig("Breast Zero",               0.100f),
                new ExampleMorphConfig("Breasts Natural Left",     -0.150f),
                new ExampleMorphConfig("Breasts Natural Right",     0.075f),
                new ExampleMorphConfig("Breasts Size",              0.050f),
                new ExampleMorphConfig("BreastsShape2",            -0.175f),
                new ExampleMorphConfig("Nipple Diameter",          -0.400f),
                new ExampleMorphConfig("Nipple Size",              -0.400f),
                new ExampleMorphConfig("Nipple Length",            -0.200f),
            });
            examples.Add("smallAndPerky", new List<ExampleMorphConfig>
            {
                //                      morph                       value
                new ExampleMorphConfig("Areolae Perk",              0.400f),
                new ExampleMorphConfig("Areola Size",              -0.300f),
                new ExampleMorphConfig("Breast diameter",          -0.050f),
                new ExampleMorphConfig("Breast Sag1",               0.100f),
                new ExampleMorphConfig("Breast Sag2",               0.150f),
                new ExampleMorphConfig("Breast Pointed",            0.300f),
                new ExampleMorphConfig("Breast Round",             -0.300f),
                new ExampleMorphConfig("Breasts Cleavage",          0.150f),
                new ExampleMorphConfig("Breasts Natural Left",      0.150f),
                new ExampleMorphConfig("Breasts Natural Right",     0.190f),
                new ExampleMorphConfig("Breasts Implants Left",    -0.025f),
                new ExampleMorphConfig("Breasts Implants Right",   -0.025f),
                new ExampleMorphConfig("Breasts Small",             0.150f),
                new ExampleMorphConfig("Breasts Under Curve",       0.150f),
                new ExampleMorphConfig("Nipple Diameter",          -0.600f),
                new ExampleMorphConfig("Nipple Length",            -0.300f),
                new ExampleMorphConfig("Nipple Size",              -0.100f),
                new ExampleMorphConfig("Sternum Width",             0.100f),
            });
            examples.Add("mediumImplants", new List<ExampleMorphConfig>
            {
                //                      morph                       value
                new ExampleMorphConfig("Breast diameter",           0.500f),
                new ExampleMorphConfig("Breast Round",             -0.250f),
                new ExampleMorphConfig("Breast Under Smoother3",   -0.350f),
                new ExampleMorphConfig("Breasts Cleavage",          0.500f),
                new ExampleMorphConfig("Breasts Implants Left",     0.200f),
                new ExampleMorphConfig("Breasts Implants Right",    0.180f),
                new ExampleMorphConfig("Breasts Natural Left",     -0.150f),
                new ExampleMorphConfig("Breasts Natural Right",    -0.125f),
                new ExampleMorphConfig("Breasts Perk Side",         0.300f),
                new ExampleMorphConfig("Breasts Size",             -0.350f),
                new ExampleMorphConfig("Chest Smoother",           -0.500f),
                new ExampleMorphConfig("Nipple Size",              -0.500f),
                new ExampleMorphConfig("Nipple Length",            -0.350f),
                new ExampleMorphConfig("Nipples Size",              0.150f),
                new ExampleMorphConfig("Sternum Width",             0.750f),
            });
            examples.Add("hugeAndSoft", new List<ExampleMorphConfig>
            {
                //                      morph                       value
                new ExampleMorphConfig("Areola Size",               0.500f),
                new ExampleMorphConfig("Areola Size X",            -0.250f),
                new ExampleMorphConfig("Areola Size Y",             0.650f),
                new ExampleMorphConfig("Areola Puffy Edge",         0.500f),
                new ExampleMorphConfig("Areolae Diameter",          0.250f),
                new ExampleMorphConfig("Areolae Perk",              0.500f),
                new ExampleMorphConfig("Breasts Cleavage",         -0.100f),
                new ExampleMorphConfig("Breasts Gone",             -0.100f),
                new ExampleMorphConfig("Breasts Perk Side",         0.450f),
                new ExampleMorphConfig("Breasts Size",             -0.100f),
                new ExampleMorphConfig("BreastsShape3",            -0.300f),
                new ExampleMorphConfig("Nipple Diameter",          -0.333f),
                new ExampleMorphConfig("Nipples Large",            -0.100f),
                new ExampleMorphConfig("ChestUnderBreast",          0.250f),
                new ExampleMorphConfig("Sternum Width",             0.250f),
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
