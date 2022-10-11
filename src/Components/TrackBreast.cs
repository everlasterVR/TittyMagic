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

        protected Vector3 breastPositionBase;
        protected float breastDepthBase;

        protected Func<Vector3> calculateBreastRelativePosition;
        protected Func<float> calculateBreastRelativeDepth;

        protected TrackBreast()
        {
            chestRb = MainPhysicsHandler.chestRb;
        }

        public void Calibrate()
        {
            breastPositionBase = calculateBreastRelativePosition();
            breastDepthBase = calculateBreastRelativeDepth();
        }

        public void UpdateAnglesAndDepthDiff()
        {
            var relativePos = calculateBreastRelativePosition();
            angleY = Vector2.SignedAngle(
                new Vector2(breastPositionBase.z, breastPositionBase.y),
                new Vector2(relativePos.z, relativePos.y)
            );
            angleX = Vector2.SignedAngle(
                new Vector2(breastPositionBase.z, breastPositionBase.x),
                new Vector2(relativePos.z, relativePos.x)
            );

            depthDiff = breastDepthBase - calculateBreastRelativeDepth();
        }

        public void ResetAnglesAndDepthDiff()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
