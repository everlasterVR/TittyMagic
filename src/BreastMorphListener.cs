//#define DEBUG_ON

using System;
using System.Collections.Generic;
using System.Linq;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class BreastMorphListener
    {
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

                    if(!_listenedMorphs.Contains(morph) && IsInSet(morph, VertexIndexGroups.BREASTS, _filterStrength))
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
