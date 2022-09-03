using System;
using TittyMagic.Handlers;
using UnityEngine;

namespace TittyMagic.Components
{
    public class TrackNipple
    {
        private readonly Rigidbody _chestRb;
        private readonly Rigidbody _pectoralRb;

        public Func<Vector3> getNipplePosition { private get; set; }

        private Vector3 _neutralRelativePosition;
        private Vector3 _neutralRelativePectoralPosition;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        public TrackNipple(Rigidbody pectoralRb)
        {
            _chestRb = MainPhysicsHandler.chestRb;
            _pectoralRb = pectoralRb;
        }

        public void Calibrate()
        {
            _neutralRelativePosition = Calc.RelativePosition(_chestRb, getNipplePosition());
            _neutralRelativePectoralPosition = Calc.RelativePosition(_chestRb, _pectoralRb.position);
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
