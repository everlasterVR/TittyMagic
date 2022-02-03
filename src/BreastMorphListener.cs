//#define DEBUG_ON

using System;
using System.Collections.Generic;
using System.Linq;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class BreastMorphListener
    {
        // does not include areola/nipple vertices
        private HashSet<int> _breastVertices = new HashSet<int>
        {
            8, 16, 17, 93, 135, 136, 137, 138, 2401, 2402, 2403, 2404, 2405, 2406, 2407, 2408, 2409, 2410, 2411, 2412, 2413, 2414, 2415, 2416, 2417, 2418, 2419, 2590, 2591, 2592, 2594, 2596, 2597, 2598, 2871, 2872, 2873, 7216, 7218, 7219, 7220, 7230, 7231, 7232, 7233, 7234, 7235, 7236, 7237, 7238, 7239, 7240, 7241, 7242, 7243, 7244, 7245, 7246, 7247, 7248, 8833, 8834, 8836, 8837, 8838, 8839, 8840, 8848, 8849, 8850, 8851, 8852, 8853, 8854, 10936, 10944, 10945, 11021, 11063, 11064, 11065, 11066, 13231, 13232, 13233, 13234, 13235, 13236, 13237, 13238, 13239, 13240, 13241, 13242, 13243, 13244, 13245, 13246, 13247, 13248, 13249, 13408, 13409, 13410, 13411, 13412, 13413, 13414, 13672, 13673, 13674, 17923, 17924, 17925, 17926, 17936, 17937, 17938, 17939, 17940, 17941, 17942, 17943, 17944, 17945, 17946, 17947, 17948, 17949, 17950, 17951, 17952, 17953, 17954, 19504, 19505, 19507, 19508, 19509, 19510, 19511, 19519, 19520, 19521, 19522, 19523, 19524, 19525, 19639
        };

        private List<string> _excludeMorphs = new List<string>
        {
            "FBMFitnessDetails",
            "FBMHeight",
            "PBMShouldersSize",
            "Alessa",
            "Eisa",
            "Izarra",
            "PBMLegsLength",
            "Shin Length",
            "Thigh Length",
            "Upper Body Length",
            "G3F_Hip Abdomen Shape",
            "bellybulge",
            "deepthroat",
            "Athletic",
            "BodyShort",
            "Hollow back",
        };

        private static float _filterStrength = 0.005f;
        private HashSet<DAZMorph> _listenedMorphs = new HashSet<DAZMorph>();
        private Dictionary<string, float> _status = new Dictionary<string, float>();

        public BreastMorphListener(List<DAZMorph> morphs)
        {
            foreach(DAZMorph morph in morphs)
            {
                try
                {
                    if(!morph.visible || morph.isPoseControl || morph.group.Contains("Pose/") || _excludeMorphs.Contains(morph.morphName))
                    {
                        continue;
                    }

                    if(!_listenedMorphs.Contains(morph) && IsInSet(morph, _breastVertices, _filterStrength))
                    {
                        _listenedMorphs.Add(morph);
                        _status.Add(morph.uid, morph.morphValue);
                    }
                }
                catch(Exception)
                {
#if DEBUG_ON
                    LogMessage($"Unable to initialize listener for morph {morph.morphName}.", nameof(BreastMorphListener));
#endif
                }
            }

#if DEBUG_ON
            LogMessage(GetStatus() + "- - - - - - - - - -\n", nameof(BreastMorphListener));
#endif
        }

        public bool Changed()
        {
            foreach(DAZMorph morph in _listenedMorphs)
            {
                float value = morph.morphValue;
                if(value != _status[morph.uid])
                {
                    _status[morph.uid] = value;
#if DEBUG_ON
                    LogMessage($"change detected! morph {MorphName(morph)}", nameof(BreastMorphListener));
#endif
                    return true;
                }
            };
            return false;
        }

        private bool IsInSet(DAZMorph morph, HashSet<int> vertices, float filterStrength)
        {
            if(morph.deltas == null)
            {
                morph.LoadDeltas();
            }
            var hitDelta = 0.0f;
            var hitDeltaMax = morph.deltas.Sum(x => x.delta.magnitude) * filterStrength;
            foreach(DAZMorphVertex delta in morph.deltas)
            {
                if(vertices.Contains(delta.vertex))
                {
                    hitDelta += delta.delta.magnitude;
                    if(hitDelta >= hitDeltaMax)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

#if DEBUG_ON

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
