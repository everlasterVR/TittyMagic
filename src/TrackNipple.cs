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
            NippleRb = nippleRb;
            _pectoralRb = pectoralRb;
        }

        public bool CalibrationDone()
        {
            bool positionCalibrated = Calc.VectorEqualWithin(
                1000000f,
                _neutralRelativePosition,
                CalculateRelativePosition()
            );
            bool depthCalibrated = Calc.EqualWithin(100000f, _neutralDepth, CalculateDepth());
            return positionCalibrated && depthCalibrated;
        }

        public void Calibrate()
        {
            _neutralRelativePosition = CalculateRelativePosition();
            _neutralDepth = CalculateDepth();
        }

        public void UpdateAnglesAndDepthDiff()
        {
            if(NippleRb?.transform == null)
            {
                return;
            }

            Vector3 relativePos = CalculateRelativePosition();
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

        private Vector3 CalculateRelativePosition()
        {
            Vector3 difference = NippleRb.position - _chestRb.position;
            Transform chestTransform = _chestRb.transform;
            return new Vector3(
                Vector3.Dot(difference, chestTransform.right.normalized),
                Vector3.Dot(difference, chestTransform.up.normalized),
                Vector3.Dot(difference, chestTransform.forward.normalized)
            );
        }

        private float CalculateDepth()
        {
            return Vector3.Distance(_pectoralRb.position, NippleRb.position);
        }
    }
}
