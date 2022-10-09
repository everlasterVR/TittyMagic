using System;
using TittyMagic.Handlers;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Components
{
    public class TrackFemaleBreast : TrackBreast
    {
        private readonly Rigidbody _nippleRb;
        private readonly Rigidbody[] _softVertexJointRBs;

        private Vector3[] _relativePositions;
        public float weightingRatio { private get; set; }

        public TrackFemaleBreast(string side)
        {
            if(side == Side.LEFT)
            {
                _nippleRb = Utils.FindRigidbody(tittyMagic.containingAtom, "lNipple");
                _softVertexJointRBs = SoftPhysicsHandler.GetLeftBreastTrackingRBs();
                calculateBreastDepth = () => PectoralRelativeDepth(tittyMagic.pectoralRbLeft);
            }
            else if(side == Side.RIGHT)
            {
                _nippleRb = Utils.FindRigidbody(tittyMagic.containingAtom, "rNipple");
                _softVertexJointRBs = SoftPhysicsHandler.GetRightBreastTrackingSets();
                calculateBreastRelativePosition = SmoothBreastRelativePosition;
                calculateBreastDepth = () => PectoralRelativeDepth(tittyMagic.pectoralRbRight);
            }

            SetCalculateBreastPosition(tittyMagic.settingsMonitor.softPhysicsEnabled);
        }

        public void SetCalculateBreastPosition(bool softPhysicsOn)
        {
            if(softPhysicsOn)
            {
                calculateBreastRelativePosition = SmoothBreastRelativePosition;
            }
            else
            {
                calculateBreastRelativePosition = () => Calc.RelativePosition(chestRb, _nippleRb.position);
            }
        }

        public void SetMovingAveragePeriod(int value)
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

        private Vector3 SmoothBreastRelativePosition()
        {
            Array.Copy(_relativePositions, 0, _relativePositions, 1, _relativePositions.Length - 1);
            _relativePositions[0] = RelativeBreastPosition();
            return Calc.ExponentialMovingAverage(_relativePositions, weightingRatio)[0];
        }

        private Vector3 RelativeBreastPosition()
        {
            var positions = new Vector3[_softVertexJointRBs.Length];
            for(int i = 0; i < _softVertexJointRBs.Length; i++)
            {
                positions[i] = _softVertexJointRBs[i].position;
            }

            return Calc.RelativePosition(chestRb, Calc.AveragePosition(positions));
        }

        private float PectoralRelativeDepth(Rigidbody pectoralRb)
        {
            var position = Calc.RelativePosition(chestRb, pectoralRb.position);
            return position.z;
        }
    }
}
