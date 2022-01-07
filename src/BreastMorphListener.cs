//#define SHOW_DEBUG

using System;
using System.Collections.Generic;

namespace TittyMagic
{
    internal class BreastMorphListener
    {
        private HashSet<int> breastVertices = new HashSet<int>() {
            0, 1, 2, 3, 4, 5, 8, 10, 11, 12, 13, 14, 15, 16, 17, 63, 77, 78, 79, 80, 93, 135, 136, 137, 138, 139, 2401, 2402, 2403, 2404, 2405, 2406, 2407, 2408, 2409, 2410, 2411, 2412, 2413, 2414, 2415, 2416, 2417, 2418, 2419, 2422, 2423, 2424, 2425, 2426, 2427, 2428, 2590, 2591, 2592, 2594, 2596, 2597, 2598, 2871, 2872, 2873, 5814, 5815, 5816, 5817, 5818, 5819, 5820, 5821, 5822, 5823, 5824, 5825, 5826, 5827, 5828, 5829, 5830, 5831, 5832, 5833, 5834, 5835, 5836, 5837, 7192, 7193, 7194, 7195, 7196, 7197, 7198, 7199, 7200, 7201, 7202, 7203, 7204, 7205, 7206, 7207, 7208, 7209, 7210, 7211, 7212, 7213, 7214, 7215, 7216, 7218, 7219, 7220, 7230, 7231, 7232, 7233, 7234, 7235, 7236, 7237, 7238, 7239, 7240, 7241, 7242, 7243, 7244, 7245, 7246, 7247, 7248, 7944, 7945, 7946, 7947, 7948, 7949, 7950, 7951, 7952, 7953, 7954, 7955, 7956, 7957, 7958, 7959, 7960, 7961, 7962, 7963, 7964, 7965, 7966, 7967, 8833, 8834, 8836, 8837, 8838, 8839, 8840, 8848, 8849, 8850, 8851, 8852, 8853, 8854, 8901, 8902, 8903, 8904, 8905, 8906, 8907, 8908, 8909, 8910, 8911, 8912, 8913, 8914, 8915, 8916, 8917, 8918, 8919, 8920, 8921, 8922, 8923, 8924, 8925, 8926, 8927, 8928, 8929, 8930, 8931, 8932, 8933, 8934, 8935, 8936, 8937, 8938, 8939, 8940, 8941, 8942, 8943, 8944, 8945, 8946, 8947, 8948, 8949, 8950, 8951, 8952, 8953, 8954, 8955, 8956, 8957, 8958, 8959, 8960, 8961, 8962, 8963, 8964, 8965, 8966, 8967, 8968, 8969, 8970, 8971, 8972, 10928, 10929, 10930, 10931, 10932, 10933, 10936, 10938, 10939, 10940, 10941, 10942, 10943, 10944, 10945, 10991, 11005, 11006, 11007, 11008, 11021, 11063, 11064, 11065, 11066, 11067, 13231, 13232, 13233, 13234, 13235, 13236, 13237, 13238, 13239, 13240, 13241, 13242, 13243, 13244, 13245, 13246, 13247, 13248, 13249, 13251, 13252, 13253, 13254, 13255, 13256, 13257, 13408, 13409, 13410, 13411, 13412, 13413, 13414, 13672, 13673, 13674, 16539, 16540, 16541, 16542, 16543, 16544, 16545, 16546, 16547, 16548, 16549, 16550, 16551, 16552, 16553, 16554, 16555, 16556, 16557, 16558, 16559, 16560, 16561, 16562, 17899, 17900, 17901, 17902, 17903, 17904, 17905, 17906, 17907, 17908, 17909, 17910, 17911, 17912, 17913, 17914, 17915, 17916, 17917, 17918, 17919, 17920, 17921, 17922, 17923, 17924, 17925, 17926, 17936, 17937, 17938, 17939, 17940, 17941, 17942, 17943, 17944, 17945, 17946, 17947, 17948, 17949, 17950, 17951, 17952, 17953, 17954, 18622, 18623, 18624, 18625, 18626, 18627, 18628, 18629, 18630, 18631, 18632, 18633, 18634, 18635, 18636, 18637, 18638, 18639, 18640, 18641, 18642, 18643, 18644, 18645, 19504, 19505, 19507, 19508, 19509, 19510, 19511, 19519, 19520, 19521, 19522, 19523, 19524, 19525, 19567, 19568, 19569, 19570, 19571, 19572, 19573, 19574, 19575, 19576, 19577, 19578, 19579, 19580, 19581, 19582, 19583, 19584, 19585, 19586, 19587, 19588, 19589, 19590, 19591, 19592, 19593, 19594, 19595, 19596, 19597, 19598, 19599, 19600, 19601, 19602, 19603, 19604, 19605, 19606, 19607, 19608, 19609, 19610, 19611, 19612, 19613, 19614, 19615, 19616, 19617, 19618, 19619, 19620, 19621, 19622, 19623, 19624, 19625, 19626, 19627, 19628, 19629, 19630, 19631, 19632, 19633, 19634, 19635, 19636, 19637, 19638, 19639
        };

        private static float filterStrength = 0.009f;

        private HashSet<DAZMorph> listenedMorphs = new HashSet<DAZMorph>();
        private Dictionary<string, float> status = new Dictionary<string, float>();

        public BreastMorphListener(List<DAZMorph> morphs)
        {
            foreach(DAZMorph morph in morphs)
            {
                try
                {
                    if(!morph.visible || morph.isPoseControl || morph.group.Contains("Pose/"))
                    {
                        continue;
                    }

                    if(!listenedMorphs.Contains(morph) && MorphCheck.IsInSet(morph, breastVertices, filterStrength))
                    {
                        listenedMorphs.Add(morph);
                        status.Add(morph.uid, morph.morphValue);
                    }
                }
                catch(Exception)
                {
#if SHOW_DEBUG
                    Log.Message($"Unable to initialize listener for morph {morph.morphName}.", nameof(BreastMorphListener));
#endif
                }
            }
#if SHOW_DEBUG
            Log.Message(GetStatus() + "- - - - - - - - - -\n", nameof(BreastMorphListener));
#endif
        }

        public bool Changed()
        {
            foreach(DAZMorph morph in listenedMorphs)
            {
                float value = morph.morphValue;
                if(value != status[morph.uid])
                {
                    status[morph.uid] = value;
#if SHOW_DEBUG
                    Log.Message($"change detected! morph {MorphName(morph)}", nameof(BreastMorphListener));
#endif
                    return true;
                }
            };
            return false;
        }

#if SHOW_DEBUG

        public string GetStatus()
        {
            string message = $"These {listenedMorphs.Count} morphs are being monitored for changes:\n";
            foreach(DAZMorph morph in listenedMorphs)
            {
                message = message + MorphName(morph) + "\n";
            }
            return message;
        }

        private string MorphName(DAZMorph morph)
        {
            string text = morph.isInPackage ? morph.packageUid + "." : "";
            text += string.IsNullOrEmpty(morph.overrideName) ? morph.displayName : morph.overrideName;
            return text;
        }

#endif
    }
}
