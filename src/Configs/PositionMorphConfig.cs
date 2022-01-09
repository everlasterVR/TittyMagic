using UnityEngine;

namespace TittyMagic
{
    internal class PositionMorphConfig
    {
        public DAZMorph morph;
        public string _name;
        public float softnessMul;
        public float massMul;

        public PositionMorphConfig(string name, float softnessMul, float massMul = 0f)
        {
            _name = name;
            this.massMul = massMul;
            this.softnessMul = softnessMul;
            morph = Globals.GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
            if(morph == null)
            {
                Log.Error($"Morph with name {name} not found!", nameof(GravityMorphConfig));
            }
        }

        public float UpdateVal(float effect, float mass, float softness)
        {
            float value = 1.25f * (
                mass * massMul * effect / 2 +
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
            return Formatting.NameValueString(_name, morph.morphValue, 1000f, 30) + "\n";
        }
    }
}
