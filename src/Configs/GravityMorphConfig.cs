namespace TittyMagic
{
    internal class GravityMorphConfig
    {
        public DAZMorph morph;
        public string name;
        private float baseMul;
        private float? scaleMul;
        private float? gravityMul;

        public GravityMorphConfig(string name, float baseMul, float? scaleMul, float? gravityMul)
        {
            this.name = name;
            this.baseMul = baseMul;
            this.scaleMul = scaleMul;
            this.gravityMul = gravityMul;
            morph = Globals.MORPH_UI.GetMorphByDisplayName(name);
            if(morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(GravityMorphConfig));
            }
        }

        public void UpdateVal(float effect, float scale, float gravity)
        {
            float scaleFactor = scaleMul.HasValue ? scale * (float) scaleMul : 1;
            float gravityFactor = gravityMul.HasValue ? gravity * (float) gravityMul : 1;
            float value = baseMul * (
                (gravityFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            morph.morphValue = value;
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
