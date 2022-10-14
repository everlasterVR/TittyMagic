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

        private float[] _relativeAnglesX;
        private float[] _relativeAnglesY;
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
                calculateBreastRelativePosition = () => Calc.RelativePosition(chestRb, CalculatePosition(_softVertexCenterJointRBs));
                calculateBreastRelativeAngleX = SmoothBreastRelativeAngleX;
                calculateBreastRelativeAngleY = SmoothBreastRelativeAngleY;
                calculateBreastRelativeDepth = SmoothBreastRelativeDepth;
            }
            else
            {
                calculateBreastRelativePosition = () => Calc.RelativePosition(chestRb, _nippleRb.position);
                calculateBreastRelativeAngleX = CalculateXAngle;
                calculateBreastRelativeAngleY = CalculateYAngle;
                calculateBreastRelativeDepth = () => Calc.RelativePosition(chestRb, _pectoralRb.position).z;
            }
        }

        public void SetMovingAveragePeriod(int value)
        {
            /* Breast relative angles X */
            {
                float[] tmpArray = new float[value];

                if(_relativeAnglesX == null)
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = 0;
                    }
                }
                else
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = i < _relativeAnglesX.Length
                            ? _relativeAnglesX[i]
                            : tmpArray[i - 1];
                    }
                }

                _relativeAnglesX = tmpArray;
            }

            /* Breast relative angles Y */
            {
                float[] tmpArray = new float[value];

                if(_relativeAnglesY == null)
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = 0;
                    }
                }
                else
                {
                    for(int i = 0; i < tmpArray.Length; i++)
                    {
                        tmpArray[i] = i < _relativeAnglesY.Length
                            ? _relativeAnglesY[i]
                            : tmpArray[i - 1];
                    }
                }

                _relativeAnglesY = tmpArray;
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

        private float SmoothBreastRelativeAngleX()
        {
            Array.Copy(_relativeAnglesX, 0, _relativeAnglesX, 1, _relativeAnglesX.Length - 1);
            _relativeAnglesX[0] = CalculateXAngle();
            return Calc.ExponentialMovingAverage(_relativeAnglesX, weightingRatio)[0];
        }

        private float SmoothBreastRelativeAngleY()
        {
            Array.Copy(_relativeAnglesY, 0, _relativeAnglesY, 1, _relativeAnglesY.Length - 1);
            _relativeAnglesY[0] = CalculateYAngle();
            return Calc.ExponentialMovingAverage(_relativeAnglesY, weightingRatio)[0];
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
