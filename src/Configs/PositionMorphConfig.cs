using static TittyMagic.Utils;

namespace TittyMagic
{
    public class PositionMorphConfig
    {
        public DAZMorph morph;
        private string _name;
        public float softnessMul;
        public float massMul;

        public string Name => _name;

        public PositionMorphConfig(string name)
        {
            _name = name;
            morph = Globals.GEOMETRY.morphsControlUI.GetMorphByDisplayName(name);
            if(morph == null)
            {
                LogError($"Morph with name {name} not found!", nameof(GravityMorphConfig));
            }
        }

        public void SetMultipliers(float softnessMul, float massMul = 0f)
        {
            this.massMul = massMul;
            this.softnessMul = softnessMul;
        }

        public float UpdateVal(float effect, float mass, float softness)
        {
            float value =
                mass * massMul * effect / 2 +
                softness * softnessMul * effect / 2;
            morph.morphValue = value;
            return value;
        }

        public void Reset()
        {
            morph.morphValue = 0;
        }
    }
}
