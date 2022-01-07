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

        public void UpdateVal(float effect, float scale, float softness)
        {
            float value =
                scale * scaleMul * effect / 2 +
                softness * softnessMul * effect / 2;
            morph.morphValue = 5 * value;
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
