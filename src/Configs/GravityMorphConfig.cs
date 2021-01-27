namespace everlaster
{
    class GravityMorphConfig
    {
        public DAZMorph morph;
        public string name;
        public string angleType;
        private float baseMul;
        private float? scaleMul;
        private float? softnessMul;

        public GravityMorphConfig(string name, string angleType, float baseMul,  float? scaleMul, float? softnessMul)
        {
            this.name = name;
            this.angleType = angleType;
            this.baseMul = baseMul;
            this.scaleMul = scaleMul;
            this.softnessMul = softnessMul;
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
            float scaleFactor = scaleMul.HasValue ? scale * (float) scaleMul : 1;
            float softnessFactor = softnessMul.HasValue ? (float) softnessMul * softness : 1;
            float sagMul = sag >= 1 ? 1 + (sag - 1) / 2 : sag;
            float morphValue = sagMul * baseMul * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            // TODO replace with SmoothStep and log based max
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
