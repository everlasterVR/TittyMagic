using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TittyMagic
{
    internal class TrackNipple
    {
        private Rigidbody _chestRb;
        private Rigidbody _pectoralRb;
        public Rigidbody NippleRb { get; set; }

        private Vector3 _neutralRelativePosition;
        private float _neutralDepth;

        public float AngleY { get; set; }
        public float AngleX { get; set; }
        public float DepthDiff { get; set; }

        public TrackNipple(Rigidbody chestRb, Rigidbody pectoralRb, Rigidbody nippleRb)
        {
            _chestRb = chestRb;
            _pectoralRb = pectoralRb;
            NippleRb = nippleRb;
        }

        public bool CalibrationDone()
        {
            bool positionCalibrated = Calc.VectorEqualWithin(
                1000000f,
                _neutralRelativePosition,
                Calc.RelativePosition(_chestRb, NippleRb.position)
            );
            bool depthCalibrated = Calc.EqualWithin(100000f, _neutralDepth, CalculateDepth());
            return positionCalibrated && depthCalibrated;
        }

        public void Calibrate()
        {
            _neutralRelativePosition = Calc.RelativePosition(_chestRb, NippleRb.position);
            _neutralDepth = CalculateDepth();
        }

        public void UpdateAnglesAndDepthDiff()
        {
            if(NippleRb?.transform == null)
            {
                return;
            }

            Vector3 relativePos = Calc.RelativePosition(_chestRb, NippleRb.position);
            AngleY = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.y),
                new Vector2(relativePos.z, relativePos.y)
            );
            AngleX = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.x),
                new Vector2(relativePos.z, relativePos.x)
            );
            DepthDiff = CalculateDepth() - _neutralDepth;
        }

        public void ResetAnglesAndDepthDiff()
        {
            AngleY = 0;
            AngleX = 0;
            DepthDiff = 0;
        }

        private float CalculateDepth()
        {
            return Vector3.Distance(_pectoralRb.position, NippleRb.position);
        }
    }
}
