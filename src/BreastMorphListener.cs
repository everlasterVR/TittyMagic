// #define DEBUG_ON

using System;
using System.Collections.Generic;
using System.Linq;

namespace TittyMagic
{
    internal class BreastMorphListener
    {
        private readonly List<string> _excludeMorphsNames = new List<string>
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
            "Body Tone",
            "Bodybuilder Details",
            "Julian Body",
            "Shoulders Width",
            "Nipples Large",
            "WeebU.My_morphs.3.SideBulge",
            "Ribs Width",
            "Rectus Width",
            "Rectus Outer Detail",
            "Distort 1",
            "Distort 2",
            "Amy AiMei",
            "succubus body",
            "Impact - Breast",
            "Breath1",
            "Ribcage undefined",
            "Sternocleidomastoid",
        };

        private const float FILTER_STRENGTH = 0.005f;
        private readonly Dictionary<DAZMorph, float> _listenedFemaleMorphs;
        private readonly Dictionary<DAZMorph, float> _listenedMaleMorphs;

        public BreastMorphListener(List<DAZMorph> femaleMorphs, List<DAZMorph> maleMorphs = null)
        {
            _listenedFemaleMorphs = ProcessMorphs(femaleMorphs);
            _listenedMaleMorphs = ProcessMorphs(maleMorphs);
#if DEBUG_ON
            SuperController.LogMessage($"Female morphs:");
            foreach(var morph in _listenedFemaleMorphs)
            {
                SuperController.LogMessage($"{morph.Key.uid}");
            }

            SuperController.LogMessage($"Male morphs:");
            foreach(var morph in _listenedMaleMorphs)
            {
                SuperController.LogMessage($"{morph.Key.uid}");
            }
#endif
        }

        private Dictionary<DAZMorph, float> ProcessMorphs(List<DAZMorph> morphs)
        {
            var listenedMorphs = new Dictionary<DAZMorph, float>();
            if(morphs == null)
            {
                return listenedMorphs;
            }

            foreach(var morph in morphs)
            {
                try
                {
                    if(!morph.visible || morph.isPoseControl || morph.group.Contains("Pose/") || _excludeMorphsNames.Contains(morph.morphName))
                    {
                        continue;
                    }

                    if(!listenedMorphs.ContainsKey(morph) && IsInSet(morph, VertexIndexGroups.BREASTS, FILTER_STRENGTH))
                    {
                        listenedMorphs.Add(morph, morph.morphValue);
                    }
                }
                catch(Exception)
                {
                    // ignored
#if DEBUG_ON
                    SuperController.LogMessage($"Unable to initialize listener for morph {morph.morphName}.");
#endif
                }
            }

            return listenedMorphs;
        }

        public bool Changed()
        {
            return MorphsChanged(_listenedFemaleMorphs) || MorphsChanged(_listenedMaleMorphs);
        }

        private static bool MorphsChanged(Dictionary<DAZMorph, float> listenedMorphs)
        {
            foreach(var listenedMorph in listenedMorphs)
            {
                var dazMorph = listenedMorph.Key;
                float value = dazMorph.morphValue;
                if(Math.Abs(value - listenedMorph.Value) > 0.001f)
                {
                    listenedMorphs[dazMorph] = value;
#if DEBUG_ON
                    SuperController.LogMessage($"change detected! morph {dazMorph.uid}");
#endif
                    return true;
                }
            }

            return false;
        }

        private static bool IsInSet(DAZMorph morph, ICollection<int> vertices, float filterStrength)
        {
            if(morph.deltas == null)
            {
                morph.LoadDeltas();
            }

            float hitDelta = 0.0f;
            float hitDeltaMax = morph.deltas.Sum(x => x.delta.magnitude) * filterStrength;
            foreach(var delta in morph.deltas)
            {
                if(!vertices.Contains(delta.vertex))
                {
                    continue;
                }

                hitDelta += delta.delta.magnitude;
                if(hitDelta >= hitDeltaMax)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
