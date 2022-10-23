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
        private Vector2 _breastAngleXBaseVector;
        private Vector2 _breastAngleYBaseVector;
        private float _breastDepthBase;

        protected Func<Vector3> calculateBreastRelativePosition;
        protected Func<float> calculateBreastRelativeAngleX;
        protected Func<float> calculateBreastRelativeAngleY;
        protected Func<float> calculateBreastRelativeDepth;

        protected TrackBreast()
        {
            chestRb = MainPhysicsHandler.chestRb;
        }

        protected float CalculateXAngle() =>
            Vector2.SignedAngle(_breastAngleXBaseVector, new Vector2(_relativePosition.z, _relativePosition.x));

        protected float CalculateYAngle() =>
            Vector2.SignedAngle(_breastAngleYBaseVector, new Vector2(_relativePosition.z, _relativePosition.y));

        public void Calibrate()
        {
            _relativePosition = calculateBreastRelativePosition();
            _breastAngleXBaseVector = new Vector2(_relativePosition.z, _relativePosition.x);
            _breastAngleYBaseVector = new Vector2(_relativePosition.z, _relativePosition.y);
            _breastDepthBase = calculateBreastRelativeDepth();
        }

        public void Update()
        {
            _relativePosition = calculateBreastRelativePosition();
            angleX = calculateBreastRelativeAngleX();
            angleY = calculateBreastRelativeAngleY();
            depthDiff = _breastDepthBase - calculateBreastRelativeDepth();
        }

        public void Reset()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
