using System;
using UnityEngine;

namespace TittyMagic.Components
{
    internal class TrackNipple
    {
        private readonly Rigidbody _chestRb;
        private readonly Rigidbody _pectoralRb;

        public Func<Vector3> getNipplePosition { private get; set; }

        private Vector3 _neutralRelativePosition;
        private Vector3 _neutralRelativePectoralPosition;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        private readonly Vector3[] _nippleCalibrationVectors = new Vector3[3];
        private readonly Vector3[] _pectoralCalibrationVectors = new Vector3[3];

        public TrackNipple(Rigidbody chestRb, Rigidbody pectoralRb)
        {
            _chestRb = chestRb;
            _pectoralRb = pectoralRb;
        }

        public void Calibrate()
        {
            _nippleCalibrationVectors.Unshift(Calc.RelativePosition(_chestRb, getNipplePosition()));
            _pectoralCalibrationVectors.Unshift(Calc.RelativePosition(_chestRb, _pectoralRb.position));
            _neutralRelativePosition = _nippleCalibrationVectors[_nippleCalibrationVectors.Length - 1];
            _neutralRelativePectoralPosition = _pectoralCalibrationVectors[_pectoralCalibrationVectors.Length - 1];
        }

        public void UpdateAnglesAndDepthDiff()
        {
            var relativePos = Calc.RelativePosition(_chestRb, getNipplePosition());
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

        public void ResetAnglesAndDepthDiff()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
