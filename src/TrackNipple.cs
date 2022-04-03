using System;
using UnityEngine;

namespace TittyMagic
{
    internal class TrackNipple
    {
        private readonly Transform _chestTransform;
        private readonly Transform _pectoralTransform;
        private readonly Transform _nippleTransform;

        private Vector3 _neutralRelativePosition;
        private Vector3 _neutralRelativePectoralPosition;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        private const int SMOOTH_UPDATES_COUNT = 4;
        private readonly float[] _zDiffs = new float[SMOOTH_UPDATES_COUNT];

        public TrackNipple(Transform chestTransform, Transform pectoralTransform, Transform nippleTransform)
        {
            _chestTransform = chestTransform;
            _pectoralTransform = pectoralTransform;
            _nippleTransform = nippleTransform;
        }

        public bool CalibrationDone()
        {
            bool positionCalibrated = Calc.VectorEqualWithin(
                1000000f,
                _neutralRelativePosition,
                Calc.RelativePosition(_chestTransform, _nippleTransform.position)
            );
            bool pectoralPositionCalibrated = Calc.VectorEqualWithin(
                1000000f,
                _neutralRelativePectoralPosition,
                Calc.RelativePosition(_chestTransform, _pectoralTransform.position)
            );
            return positionCalibrated && pectoralPositionCalibrated;
        }

        public void Calibrate()
        {
            _neutralRelativePosition = Calc.RelativePosition(_chestTransform, _nippleTransform.position);
            _neutralRelativePectoralPosition = Calc.RelativePosition(_chestTransform, _pectoralTransform.position);
        }

        public void UpdateAnglesAndDepthDiff()
        {
            var relativePos = Calc.RelativePosition(_chestTransform, _nippleTransform.position);
            angleY = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.y),
                new Vector2(relativePos.z, relativePos.y)
            );
            angleX = Vector2.SignedAngle(
                new Vector2(_neutralRelativePosition.z, _neutralRelativePosition.x),
                new Vector2(relativePos.z, relativePos.x)
            );

            depthDiff = ExpAverageDepthDiff();
        }

        private float ExpAverageDepthDiff()
        {
            Array.Copy(_zDiffs, 0, _zDiffs, 1, _zDiffs.Length - 1);
            var relativePectoralPos = Calc.RelativePosition(_chestTransform, _pectoralTransform.position);
            _zDiffs[0] = (_neutralRelativePectoralPosition - relativePectoralPos).z;
            return Calc.ExponentialMovingAverage(_zDiffs, 0.75f)[0];
        }

        public void ResetAnglesAndDepthDiff()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
