using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Components
{
    public class TrackFutaBreast : TrackBreast
    {
        private readonly int[] _nippleVertexIndices;

        public TrackFutaBreast(string side)
        {
            if(side == Side.LEFT)
            {
                _nippleVertexIndices = VertexIndexGroup.leftNippleMale;
                calculateBreastRelativePosition = RelativeNippleSkinPosition;
                calculateBreastDepth = () => PectoralRelativeDepth(tittyMagic.pectoralRbLeft);
            }
            else if(side == Side.RIGHT)
            {
                _nippleVertexIndices = VertexIndexGroup.rightNippleMale;
                calculateBreastRelativePosition = RelativeNippleSkinPosition;
                calculateBreastDepth = () => PectoralRelativeDepth(tittyMagic.pectoralRbRight);
            }
        }

        private Vector3 RelativeNippleSkinPosition()
        {
            var vertices = new Vector3[_nippleVertexIndices.Length];
            for(int i = 0; i < _nippleVertexIndices.Length; i++)
            {
                vertices[i] = skin.rawSkinnedVerts[_nippleVertexIndices[i]];
            }

            return Calc.RelativePosition(chestRb.transform, Calc.AveragePosition(vertices));
        }

        private float PectoralRelativeDepth(Rigidbody pectoralRb)
        {
            var position = Calc.RelativePosition(chestRb, pectoralRb.position);
            return position.z;
        }
    }
}
