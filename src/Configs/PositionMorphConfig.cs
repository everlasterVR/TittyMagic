using UnityEngine;

namespace TittyMagic
{
    internal class PositionMorphConfig
    {
        public DAZMorph morph;
        public string name;
        private float scaleMul;
        private float softnessMul;

        public PositionMorphConfig(string name, float softnessMul, float scaleMul = 0f)
        {
            this.name = name;
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
            float interpolatedEffect = Mathf.SmoothStep(0, Calc.ScaledSmoothMax(softness, logMaxX), effect * 5);
            float value =
                scale * scaleMul * interpolatedEffect / 2 +
                softness * softnessMul * interpolatedEffect / 2;

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
