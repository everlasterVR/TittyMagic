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
                _nippleVertexIndices = new[] { 8911, 8930, 8943, 8947 };
                calculateBreastRelativeDepth = () => Calc.RelativePosition(chestRb, tittyMagic.pectoralRbLeft.position).z;
            }
            else if(side == Side.RIGHT)
            {
                _nippleVertexIndices = new[] { 19577, 19596, 19609, 19625 };
                calculateBreastRelativeDepth = () => Calc.RelativePosition(chestRb, tittyMagic.pectoralRbRight.position).z;
            }

            calculateBreastRelativeAngleX = CalculateXAngle;
            calculateBreastRelativeAngleY = CalculateYAngle;
            calculateBreastRelativePosition = RelativeNippleSkinPosition;
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
    }
}
