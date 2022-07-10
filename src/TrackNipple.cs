using System;
using System.Linq;
using UnityEngine;

namespace TittyMagic
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

        private const int SMOOTH_UPDATES_COUNT = 4;
        private readonly float[] _zDiffs = new float[SMOOTH_UPDATES_COUNT];

        private Vector3[] _nippleCalibrationVectors = new Vector3[3];
        private Vector3[] _pectoralCalibrationVectors = new Vector3[3];

        public TrackNipple(Rigidbody chestRb, Rigidbody pectoralRb)
        {
            _chestRb = chestRb;
            _pectoralRb = pectoralRb;
        }

        public bool CalibrationDone()
        {
            if(_nippleCalibrationVectors[_nippleCalibrationVectors.Length - 1] == Vector3.zero)
            {
                return false;
            }

            bool result = CheckNipple() && CheckPectoral();
            if(result)
            {
                _nippleCalibrationVectors = new Vector3[3];
                _pectoralCalibrationVectors = new Vector3[3];
            }

            return result;
        }

        private bool CheckNipple()
        {
            var newPos = Calc.RelativePosition(_chestRb, getNipplePosition());
            return Calc.VectorEqualWithin(1000f, _nippleCalibrationVectors[0], newPos)
                && Calc.VectorEqualWithin(1000f, _nippleCalibrationVectors[1], newPos)
                && Calc.VectorEqualWithin(1000f, _nippleCalibrationVectors[2], newPos);
        }

        private bool CheckPectoral()
        {
            var newPos = Calc.RelativePosition(_chestRb, _pectoralRb.position);
            return Calc.VectorEqualWithin(1000f, _pectoralCalibrationVectors[0], newPos)
                && Calc.VectorEqualWithin(1000f, _pectoralCalibrationVectors[1], newPos)
                && Calc.VectorEqualWithin(1000f, _pectoralCalibrationVectors[2], newPos);
        }

        public void Calibrate()
        {
            _nippleCalibrationVectors.Unshift(Calc.RelativePosition(_chestRb, getNipplePosition()));
            _pectoralCalibrationVectors.Unshift(Calc.RelativePosition(_chestRb, _pectoralRb.position));
            _neutralRelativePosition = _nippleCalibrationVectors[_nippleCalibrationVectors.Length - 1];
            _neutralRelativePectoralPosition = _pectoralCalibrationVectors[_pectoralCalibrationVectors.Length - 1];
        }

        private static void Unshift<T>(T[] array, T value)
        {
            Array.Copy(array, 0, array, 1, array.Length - 1);
            array[0] = value;
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

            depthDiff = ExpAverageDepthDiff();
        }

        private float ExpAverageDepthDiff()
        {
            Array.Copy(_zDiffs, 0, _zDiffs, 1, _zDiffs.Length - 1);
            var relativePectoralPos = Calc.RelativePosition(_chestRb, _pectoralRb.position);
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
