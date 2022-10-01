using System;
using TittyMagic.Handlers;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic.Components
{
    public class TrackBreast
    {
        private readonly Rigidbody _chestRb;
        private readonly Rigidbody _pectoralRb;
        private readonly Rigidbody[] _softVertexJointRBs;
        private readonly int[] _nippleVertexIndices;
        private readonly Func<Vector3> _calculateRelativePosition;

        private Vector3[] _relativePositions;
        public float weightingRatio { private get; set; }

        private Vector3 _neutralRelativePosition;
        private Vector3 _neutralRelativePectoralPosition;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        public TrackBreast(string side)
        {
            _chestRb = MainPhysicsHandler.chestRb;

            switch(side)
            {
                case Side.LEFT:
                    _pectoralRb = tittyMagic.pectoralRbLeft;
                    if(personIsFemale)
                    {
                        _softVertexJointRBs = SoftPhysicsHandler.GetLeftBreastTrackingRBs();
                        _calculateRelativePosition = SmoothRelativePosition;
                    }
                    else
                    {
                        _nippleVertexIndices = VertexIndexGroup.leftNippleMale;
                        _calculateRelativePosition = RelativeNippleSkinPosition;
                    }

                    break;
                case Side.RIGHT:
                    _pectoralRb = tittyMagic.pectoralRbRight;
                    if(personIsFemale)
                    {
                        _softVertexJointRBs = SoftPhysicsHandler.GetRightBreastTrackingSets();
                        _calculateRelativePosition = SmoothRelativePosition;
                    }
                    else
                    {
                        _nippleVertexIndices = VertexIndexGroup.rightNippleMale;
                        _calculateRelativePosition = RelativeNippleSkinPosition;
                    }

                    break;
            }
        }

        public void SetMovingAveragePeriod(int value)
        {
            var tmpArray = new Vector3[value];

            if(_relativePositions == null)
            {
                for(int i = 0; i < tmpArray.Length; i++)
                {
                    tmpArray[i] = _neutralRelativePosition;
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

        public void Calibrate()
        {
            _neutralRelativePosition = _calculateRelativePosition();
            _neutralRelativePectoralPosition = Calc.RelativePosition(_chestRb, _pectoralRb.position);
        }

        public void UpdateAnglesAndDepthDiff()
        {
            var relativePos = _calculateRelativePosition();
            angleY = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.y),
                new Vector2(relativePos.z, relativePos.y)
            );
            angleX = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.x),
                new Vector2(relativePos.z, relativePos.x)
            );

            depthDiff = (_neutralRelativePectoralPosition - Calc.RelativePosition(_chestRb, _pectoralRb.position)).z;
        }

        private Vector3 SmoothRelativePosition()
        {
            Array.Copy(_relativePositions, 0, _relativePositions, 1, _relativePositions.Length - 1);
            _relativePositions[0] = RelativeBreastPosition();
            return Calc.ExponentialMovingAverage(_relativePositions, weightingRatio)[0];
        }

        private Vector3 RelativeBreastPosition()
        {
            var vertices = new Vector3[_softVertexJointRBs.Length];
            for(int i = 0; i < _softVertexJointRBs.Length; i++)
            {
                vertices[i] = _softVertexJointRBs[i].position;
            }

            return Calc.RelativePosition(_chestRb, Calc.AveragePosition(vertices));
        }

        private Vector3 RelativeNippleSkinPosition()
        {
            var vertices = new Vector3[_nippleVertexIndices.Length];
            for(int i = 0; i < _nippleVertexIndices.Length; i++)
            {
                vertices[i] = skin.rawSkinnedVerts[_nippleVertexIndices[i]];
            }

            return Calc.RelativePosition(_chestRb.transform, Calc.AveragePosition(vertices));
        }

        public void ResetAnglesAndDepthDiff()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
