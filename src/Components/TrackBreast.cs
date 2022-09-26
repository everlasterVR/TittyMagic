using System.Collections.Generic;
using System.Linq;
using TittyMagic.Handlers;
using UnityEngine;

namespace TittyMagic.Components
{
    public class TrackBreast
    {
        private readonly Rigidbody _chestRb;
        private readonly Transform _chestTransform;
        private readonly Rigidbody _pectoralRb;
        private readonly int[] _vertexIndexGroup;

        private Vector3 _neutralRelativePosition;
        private Vector3 _neutralRelativePectoralPosition;

        public float angleY { get; private set; }
        public float angleX { get; private set; }
        public float depthDiff { get; private set; }

        public TrackBreast(Rigidbody pectoralRb, int[] vertexIndexGroup)
        {
            _chestRb = MainPhysicsHandler.chestRb;
            _chestTransform = _chestRb.transform;
            _pectoralRb = pectoralRb;
            _vertexIndexGroup = vertexIndexGroup;
        }

        public void Calibrate()
        {
            _neutralRelativePosition = RelativeBreastPosition();
            _neutralRelativePectoralPosition = Calc.RelativePosition(_chestRb, _pectoralRb.position);
        }

        public void UpdateAnglesAndDepthDiff()
        {
            var relativePos = RelativeBreastPosition();
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

        private Vector3 RelativeBreastPosition()
        {
            var vertices = new Vector3[_vertexIndexGroup.Length];
            for(int i = 0; i < _vertexIndexGroup.Length; i++)
            {
                vertices[i] = Script.skin.rawSkinnedVerts[_vertexIndexGroup[i]];
            }

            return Calc.RelativePosition(_chestTransform, Calc.AveragePosition(vertices));
        }

        public void ResetAnglesAndDepthDiff()
        {
            angleY = 0;
            angleX = 0;
            depthDiff = 0;
        }
    }
}
