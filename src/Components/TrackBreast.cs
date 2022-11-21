using System;
using TittyMagic.Handlers;
using UnityEngine;

namespace TittyMagic.Components
{
    public class TrackBreast
    {
        protected readonly Rigidbody chestRb;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        private Vector3 _relativePosition;
        private Vector2 _angleXBaseVector;
        private Vector2 _angleYBaseVector;
        private float _depthBase;

        protected Func<Vector3> calculateRelativePosition;
        protected Func<float> calculateRelativeAngleX;
        protected Func<float> calculateRelativeAngleY;
        protected Func<float> calculateRelativeDepth;

        protected TrackBreast()
        {
            chestRb = MainPhysicsHandler.chestRb;
        }

        protected float CalculateXAngle() =>
            Vector2.SignedAngle(_angleXBaseVector, new Vector2(_relativePosition.z, _relativePosition.x));

        protected float CalculateYAngle() =>
            Vector2.SignedAngle(_angleYBaseVector, new Vector2(_relativePosition.z, _relativePosition.y));

        public void Calibrate()
        {
            _relativePosition = calculateRelativePosition();
            _angleXBaseVector = new Vector2(_relativePosition.z, _relativePosition.x);
            _angleYBaseVector = new Vector2(_relativePosition.z, _relativePosition.y);
            _depthBase = calculateRelativeDepth();
        }

        public void Update()
        {
            _relativePosition = calculateRelativePosition();
            angleX = calculateRelativeAngleX();
            angleY = calculateRelativeAngleY();
            depthDiff = _depthBase - calculateRelativeDepth();
        }

        public void Reset()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
