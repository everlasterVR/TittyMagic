using System;
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
        private Vector3 _neutralRelativePectoralPosition;

        public float AngleY { get; set; }
        public float AngleX { get; set; }
        public float DepthDiff { get; set; }

        private const int smoothUpdatesCount = 5;
        private float[] _zDiffs = new float[smoothUpdatesCount];

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
            bool pectoralPositionCalibrated = Calc.VectorEqualWithin(
                1000000f,
                _neutralRelativePectoralPosition,
                Calc.RelativePosition(_chestRb, _pectoralRb.position)
            );
            return positionCalibrated && pectoralPositionCalibrated;
        }

        public void Calibrate()
        {
            _neutralRelativePosition = Calc.RelativePosition(_chestRb, NippleRb.position);
            _neutralRelativePectoralPosition = Calc.RelativePosition(_chestRb, _pectoralRb.position);
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

            Array.Copy(_zDiffs, 0, _zDiffs, 1, _zDiffs.Length - 1);
            Vector3 relativePectoralPos = Calc.RelativePosition(_chestRb, _pectoralRb.position);
            _zDiffs[0] = (_neutralRelativePectoralPosition - relativePectoralPos).z;
            DepthDiff = Calc.ExponentialMovingAverage(_zDiffs, 0.75f)[0];
        }

        public void ResetAnglesAndDepthDiff()
        {
            AngleY = 0;
            AngleX = 0;
            DepthDiff = 0;
        }
    }
}
