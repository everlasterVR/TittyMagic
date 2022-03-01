using System;
using UnityEngine;

namespace TittyMagic
{
    internal class TrackNipple
    {
        private readonly Rigidbody _chestRb;
        private readonly Rigidbody _pectoralRb;
        public Rigidbody nippleRb { get; set; }

        private Vector3 _neutralRelativePosition;
        private Vector3 _neutralRelativePectoralPosition;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        private const int SMOOTH_UPDATES_COUNT = 4;
        private readonly float[] _zDiffs = new float[SMOOTH_UPDATES_COUNT];

        public TrackNipple(Rigidbody chestRb, Rigidbody pectoralRb, Rigidbody nippleRb)
        {
            _chestRb = chestRb;
            _pectoralRb = pectoralRb;
            this.nippleRb = nippleRb;
        }

        public bool CalibrationDone()
        {
            bool positionCalibrated = Calc.VectorEqualWithin(
                1000000f,
                _neutralRelativePosition,
                Calc.RelativePosition(_chestRb, nippleRb.position)
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
            _neutralRelativePosition = Calc.RelativePosition(_chestRb, nippleRb.position);
            _neutralRelativePectoralPosition = Calc.RelativePosition(_chestRb, _pectoralRb.position);
        }

        public void UpdateAnglesAndDepthDiff()
        {
            if(!HasTransform())
            {
                return;
            }

            var relativePos = Calc.RelativePosition(_chestRb, nippleRb.position);
            angleY = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.y),
                new Vector2(relativePos.z, relativePos.y)
            );
            angleX = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.x),
                new Vector2(relativePos.z, relativePos.x)
            );

            Array.Copy(_zDiffs, 0, _zDiffs, 1, _zDiffs.Length - 1);
            var relativePectoralPos = Calc.RelativePosition(_chestRb, _pectoralRb.position);
            _zDiffs[0] = (_neutralRelativePectoralPosition - relativePectoralPos).z;
            depthDiff = Calc.ExponentialMovingAverage(_zDiffs, 0.75f)[0];
        }

        public void ResetAnglesAndDepthDiff()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }

        public bool HasTransform()
        {
            return nippleRb != null && nippleRb.transform != null;
        }
    }
}
