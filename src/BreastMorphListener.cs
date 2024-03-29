﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TittyMagic
{
    public static class BreastMorphListener
    {
        private static HashSet<string> _excludeMorphsNames = new HashSet<string>
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
            "FBMBodyTone",
            "FBMBodyBuilderDetails",
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

        public static void ProcessMorphs(DAZMorphBank morphBank)
        {
            if(morphBank.morphs == null)
            {
                return;
            }

            var listenedMorphs = new Dictionary<DAZMorph, float>();
            foreach(var morph in morphBank.morphs)
            {
                try
                {
                    if(
                        morph.visible &&
                        !morph.isPoseControl &&
                        morph.group != null &&
                        !morph.group.Contains("Pose/") &&
                        !_excludeMorphsNames.Contains(morph.morphName) &&
                        !listenedMorphs.ContainsKey(morph) &&
                        InSet(morph, VertexIndexGroup.breasts, FILTER_STRENGTH)
                    )
                    {
                        listenedMorphs.Add(morph, morph.morphValue);
                    }
                }
                catch(Exception e)
                {
                    Utils.Log($"Unable to add morph '{morph.morphName}'. Error: {e}");
                }
            }

            if(_morphs == null)
            {
                _morphs = listenedMorphs.Keys.ToArray();
                _values = listenedMorphs.Values.ToArray();
            }
            else
            {
                var tmpMorphs = _morphs.ToList();
                tmpMorphs.AddRange(listenedMorphs.Keys);
                _morphs = tmpMorphs.ToArray();
                var tmpValues = _values.ToList();
                tmpValues.AddRange(listenedMorphs.Values);
                _values = tmpValues.ToArray();
            }
        }

        private static DAZMorph[] _morphs;
        private static float[] _values;

        public static bool ChangeWasDetected()
        {
            for(int i = 0; i < _morphs.Length; i++)
            {
                var dazMorph = _morphs[i];
                float oldValue = _values[i];
                float newValue = dazMorph.morphValue;
                if(Math.Abs(newValue - oldValue) > 0.001f)
                {
                    _values[i] = newValue;
                    return true;
                }
            }

            return false;
        }

        private static bool InSet(DAZMorph morph, ICollection<int> vertices, float filterStrength)
        {
            if(morph.deltas == null)
            {
                morph.LoadDeltas();
            }

            float hitDelta = 0.0f;
            float hitDeltaMax = morph.deltas.Sum(x => x.delta.magnitude) * filterStrength;
            foreach(var delta in morph.deltas)
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

        public static void Destroy()
        {
            _excludeMorphsNames = null;
            _morphs = null;
            _values = null;
        }
    }
}
