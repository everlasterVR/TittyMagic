using UnityEngine;

namespace TittyMagic
{
    internal class PositionMorphConfig
    {
        public DAZMorph morph;
        public string name;
        private float baseMul;
        private float scaleMul;
        private float softnessMul;

        public PositionMorphConfig(string name, float baseMul, float scaleMul, float softnessMul)
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

        public void UpdateVal(float effect, float scale, float softness, float logMaxX)
        {
            float interpolatedEffect = Mathf.SmoothStep(0, Calc.ScaledSmoothMax(scale * softness, logMaxX), effect * 3);
            float value = baseMul * (
                scale * scaleMul * interpolatedEffect / 2 +
                softness * softnessMul * interpolatedEffect / 2
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
