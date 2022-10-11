using System;
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

        private Vector3[] _relativePositions;
        private float[] _relativeDepths;
        public float weightingRatio { private get; set; }

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
            _softVertexTipJointRBs = SoftPhysicsHandler.GetBreastTipTrackingRigidbodies(side);
            _softVertexOuterJointRBs = SoftPhysicsHandler.GetTrackingRigidbodies(side, SoftColliderGroup.OUTER);

            SetCalculateFunctions(tittyMagic.settingsMonitor.softPhysicsEnabled);
        }

        public void SetCalculateFunctions(bool softPhysicsOn)
        {
            if(softPhysicsOn)
            {
                calculateBreastRelativePosition = SmoothBreastRelativePosition;
                calculateBreastRelativeDepth = SmoothBreastRelativeDepth;
            }
            else
            {
                calculateBreastRelativePosition = () => Calc.RelativePosition(chestRb, _nippleRb.position);
                calculateBreastRelativeDepth = () => Calc.RelativePosition(chestRb, _pectoralRb.position).z;
            }
        }

        public void SetMovingAveragePeriod(int value)
        {
            /* Breast relative positions */
            {
                var tmpArray = new Vector3[value];

                if(_relativePositions == null)
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = breastPositionBase;
                    }
                }
                else
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = i < _relativePositions.Length
                            ? _relativePositions[i]
                            : tmpArray[i - 1];
                    }
                }

                _relativePositions = tmpArray;
            }

            /* Breast relative depths */
            {
                float[] tmpArray = new float[value];

                if(_relativeDepths == null)
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = breastDepthBase;
                    }
                }
                else
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = i < _relativeDepths.Length
                            ? _relativeDepths[i]
                            : tmpArray[i - 1];
                    }
                }

                _relativeDepths = tmpArray;
            }
        }

        private Vector3 SmoothBreastRelativePosition()
        {
            Array.Copy(_relativePositions, 0, _relativePositions, 1, _relativePositions.Length - 1);
            _relativePositions[0] = Calc.RelativePosition(chestRb, CalculatePosition(_softVertexCenterJointRBs));
            return Calc.ExponentialMovingAverage(_relativePositions, weightingRatio)[0];
        }

        private float SmoothBreastRelativeDepth()
        {
            var tipToPectoral = Calc.RelativePosition(_pectoralRb, CalculatePosition(_softVertexTipJointRBs));
            var outerToPectoral = Calc.RelativePosition(_pectoralRb, CalculatePosition(_softVertexOuterJointRBs));

            Array.Copy(_relativeDepths, 0, _relativeDepths, 1, _relativeDepths.Length - 1);
            _relativeDepths[0] =
                0.5f * Calc.RelativePosition(chestRb, _pectoralRb.position).z
                + 0.5f * (tipToPectoral - outerToPectoral).z;

            return Calc.ExponentialMovingAverage(_relativeDepths, weightingRatio)[0];
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
