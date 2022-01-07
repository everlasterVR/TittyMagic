using UnityEngine;

namespace TittyMagic
{
    internal class PositionMorphConfig
    {
        public DAZMorph morph;
        public string name;
        private float softnessMul;
        private float scaleMul;

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

        public void SetSoftnessMul(float softnessMul)
        {
            this.softnessMul = softnessMul;
        }

        public void SetScaleMul(float scaleMul)
        {
            this.scaleMul = scaleMul;
        }

        public float UpdateVal(float effect, float scale, float softness)
        {
            float value = 5 * (
                scale * scaleMul * effect / 2 +
                softness * softnessMul * effect / 2
            );
            morph.morphValue = value;
            return value;
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
