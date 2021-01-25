using System.Collections.Generic;

namespace everlaster
{
    // TODO refactor to not use Dictionary
    class GravityMorphConfig
    {
        public DAZMorph morph;
        public string name;
        public string angleType;
        private float baseMultiplier;
        private float? scaleMultiplier;
        private float? softnessMultiplier;

        public GravityMorphConfig(string name, string angleType, float baseMultiplier,  float? scaleMultiplier, float? softnessMultiplier)
        {
            this.name = name;
            this.angleType = angleType;
            this.baseMultiplier = baseMultiplier;
            this.scaleMultiplier = scaleMultiplier;
            this.softnessMultiplier = softnessMultiplier;
            morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            if(morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(GravityMorphConfig));
            }
        }

        public void UpdateVal(float effect, float scale, float softness, float sag)
        {
            // baseMultiplier is the base multiplier for the morph in this type (UPRIGHT etc.)
            // scaleMultiplier scales the breast softness slider for this base multiplier
            //      - if null, slider setting is ignored
            // softnessMultiplier scales the size calibration slider for this base multiplier
            //      - if null, slider setting is ignored
            float scaleFactor = scaleMultiplier.HasValue ? scale * (float) scaleMultiplier : 1;
            float softnessFactor = softnessMultiplier.HasValue ? (float) softnessMultiplier * softness : 1;
            float sagMultiplierVal = sag >= 1 ? 1 + (sag - 1) / 2 : sag;
            float morphValue = sagMultiplierVal * baseMultiplier * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            if(morphValue > 0)
            {
                morph.morphValue = morphValue >= 1.33f ? 1.33f : morphValue;
            }
            else
            {
                morph.morphValue = morphValue < -1.33f ? -1.33f : morphValue;
            }
        }

        public void Reset()
        {
            morph.morphValue = 0;
        }
    }
}
