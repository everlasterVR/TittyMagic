namespace everlaster
{
    class GravityMorphConfig
    {
        public DAZMorph morph;
        public string name;
        private float offset;
        private float baseMul;
        private float? scaleMul;
        private float? softnessMul;

        public GravityMorphConfig(string name, float offset, float baseMul,  float? scaleMul, float? softnessMul)
        {
            this.name = name;
            this.offset = offset;
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
            float softnessFactor = softnessMul.HasValue ? softness * (float) softnessMul : 1;
            float value = baseMul * (
                (sag * softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            morph.morphValue = offset + value;
        }

        public void Reset()
        {
            morph.morphValue = 0;
        }

        public string GetStatus()
        {
            return Formatting.NameValueString(name, morph.morphValue, 1000f, 30) + "\n";
        }
    }
}
