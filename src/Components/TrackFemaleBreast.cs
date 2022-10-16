using TittyMagic.Handlers;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Components
{
    public class TrackFemaleBreast : TrackBreast
    {
        private readonly Rigidbody _nippleRb;
        private readonly Rigidbody _pectoralRb;

        private readonly Rigidbody[] _softVertexCenterJointRBs;
        private readonly Rigidbody[] _softVertexTipJointRBs;
        private readonly Rigidbody[] _softVertexOuterJointRBs;

        public TrackFemaleBreast(string side)
        {
            if(side == Side.LEFT)
            {
                _nippleRb = Utils.FindRigidbody(tittyMagic.containingAtom, "lNipple");
                _pectoralRb = tittyMagic.pectoralRbLeft;
            }
            else if(side == Side.RIGHT)
            {
                _nippleRb = Utils.FindRigidbody(tittyMagic.containingAtom, "rNipple");
                _pectoralRb = tittyMagic.pectoralRbRight;
            }

            _softVertexCenterJointRBs = SoftPhysicsHandler.GetBreastCenterTrackingRigidbodies(side);
            _softVertexTipJointRBs = SoftPhysicsHandler.GetTrackingRigidbodies(side, SoftColliderGroup.AREOLA);
            _softVertexOuterJointRBs = SoftPhysicsHandler.GetTrackingRigidbodies(side, SoftColliderGroup.OUTER);

            SetCalculateFunctions(tittyMagic.settingsMonitor.softPhysicsEnabled);
        }

        public void SetCalculateFunctions(bool softPhysicsOn)
        {
            if(softPhysicsOn)
            {
                calculateBreastRelativePosition = () => Calc.RelativePosition(chestRb, CalculatePosition(_softVertexCenterJointRBs));
                calculateBreastRelativeDepth = CalculateDepth;
            }
            else
            {
                calculateBreastRelativePosition = () => Calc.RelativePosition(chestRb, _nippleRb.position);
                calculateBreastRelativeDepth = () => Calc.RelativePosition(chestRb, _pectoralRb.position).z;
            }

            calculateBreastRelativeAngleX = CalculateXAngle;
            calculateBreastRelativeAngleY = CalculateYAngle;
        }

        private float CalculateDepth()
        {
            var tipToPectoral = Calc.RelativePosition(_pectoralRb, CalculatePosition(_softVertexTipJointRBs));
            var outerToPectoral = Calc.RelativePosition(_pectoralRb, CalculatePosition(_softVertexOuterJointRBs));

            return
                0.5f * Calc.RelativePosition(chestRb, _pectoralRb.position).z +
                0.5f * (tipToPectoral - outerToPectoral).z;
        }

        private static Vector3 CalculatePosition(Rigidbody[] rigidbodies)
        {
            var positions = new Vector3[rigidbodies.Length];
            for(int i = 0; i < rigidbodies.Length; i++)
            {
                positions[i] = rigidbodies[i].position;
            }

            return Calc.AveragePosition(positions);
        }
    }
}
