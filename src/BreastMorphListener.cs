// #define DEBUG_ON

using System;
using System.Collections.Generic;
using System.Linq;

namespace TittyMagic
{
    internal class BreastMorphListener
    {
        private readonly List<string> _excludeMorphs = new List<string>
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
        private readonly HashSet<DAZMorph> _listenedMorphs = new HashSet<DAZMorph>();
        private readonly Dictionary<string, float> _status = new Dictionary<string, float>();

        public BreastMorphListener(List<DAZMorph> femaleMorphs, List<DAZMorph> maleMorphs)
        {
            ProcessMorphs(femaleMorphs);
            ProcessMorphs(maleMorphs);
#if DEBUG_ON
            SuperController.LogMessage(GetStatus() + "- - - - - - - - - -\n");
#endif
        }

        private void ProcessMorphs(List<DAZMorph> morphs)
        {
            foreach(var morph in morphs)
            {
                try
                {
                    if(!morph.visible || morph.isPoseControl || morph.group.Contains("Pose/") || _excludeMorphs.Contains(morph.morphName))
                    {
                        continue;
                    }

                    if(!_listenedMorphs.Contains(morph) && IsInSet(morph, VertexIndexGroups.BREASTS, FILTER_STRENGTH))
                    {
                        _listenedMorphs.Add(morph);
                        _status.Add(morph.uid, morph.morphValue);
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
        }

        public bool Changed()
        {
            foreach(var morph in _listenedMorphs)
            {
                float value = morph.morphValue;
                if(Math.Abs(value - _status[morph.uid]) > 0.001f)
                {
                    _status[morph.uid] = value;
#if DEBUG_ON
                    SuperController.LogMessage($"change detected! morph {MorphName(morph)}");
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

#if DEBUG_ON
        private string GetStatus()
        {
            string message = $"These {_listenedMorphs.Count} morphs are being monitored for changes:\n";
            foreach(var morph in _listenedMorphs)
            {
                message += MorphName(morph) + "\n";
            }

            return message;
        }

        private static string MorphName(DAZMorph morph)
        {
            string text = morph.isInPackage ? morph.packageUid + "." : "";
            text += string.IsNullOrEmpty(morph.overrideName) ? morph.displayName : morph.overrideName;
            return text;
        }
#endif
    }
}
