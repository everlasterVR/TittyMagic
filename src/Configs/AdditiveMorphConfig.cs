namespace TittyMagic
{
    internal class AdditiveMorphConfig
    {
        public DAZMorph morph;
        public string name;
        private float baseMul;
        private float? scaleMul;
        private float? softnessMul;

        public AdditiveMorphConfig(string name, float baseMul, float? scaleMul, float? softnessMul)
        {
            this.name = name;
            this.baseMul = baseMul;
            this.scaleMul = scaleMul;
            this.softnessMul = softnessMul;
            morph = Globals.GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
            if(morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(GravityMorphConfig));
            }
        }

        public void UpdateVal(float effect, float scale, float softness)
        {
            float scaleFactor = scaleMul.HasValue ? scale * (float) scaleMul : 1;
            float softnessFactor = softnessMul.HasValue ? softness * (float) softnessMul : 1;
            float value = baseMul * (
                (softnessFactor * effect / 2) +
                (scaleFactor * effect / 2)
            );

            morph.morphValue += value;
        }
    }
}
