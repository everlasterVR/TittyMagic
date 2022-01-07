using System.Collections.Generic;
using System.Linq;

namespace TittyMagic
{
    internal class MorphCheck
    {
        public static bool IsInSet(DAZMorph morph, HashSet<int> vertices, float filterStrength)
        {
            if(morph.deltas == null)
            {
                morph.LoadDeltas();
            }
            var hitDelta = 0.0f;
            var hitDeltaMax = morph.deltas.Sum(x => x.delta.magnitude) * filterStrength;
            foreach(DAZMorphVertex delta in morph.deltas)
            {
                if(vertices.Contains(delta.vertex))
                {
                    hitDelta += delta.delta.magnitude;
                    if(hitDelta >= hitDeltaMax)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
