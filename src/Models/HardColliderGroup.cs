using TittyMagic.Components;
using UnityEngine;

namespace TittyMagic.Models
{
    public class HardColliderGroup
    {
        private const float DEFAULT_MASS = 0.04f; // Seems to be a hard coded value in VaM.

        public string id { get; }
        public string visualizerId { get; }

        private readonly Scaler _baseRbMassScaler;
        private readonly Scaler _radiusScaler;
        private readonly Scaler _rightOffsetScaler;
        private readonly Scaler _upOffsetScaler;
        private readonly Scaler _lookOffsetScaler;

        private readonly float _frictionMultiplier;

        public HardCollider left { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        public HardCollider right { get; }

        public JSONStorableFloat forceJsf { get; set; }
        public JSONStorableFloat radiusJsf { get; set; }
        public JSONStorableFloat rightJsf { get; set; }
        public JSONStorableFloat upJsf { get; set; }
        public JSONStorableFloat lookJsf { get; set; }

        public HardColliderGroup(
            string id,
            string visualizerId,
            HardCollider left,
            HardCollider right,
            Scaler baseRbMassScaler,
            Scaler radiusScaler,
            Scaler rightOffsetScaler,
            Scaler upOffsetScaler,
            Scaler lookOffsetScaler,
            float frictionMultiplier
        )
        {
            this.id = id;
            this.visualizerId = visualizerId;
            this.left = left;
            this.right = right;
            _baseRbMassScaler = baseRbMassScaler;
            _radiusScaler = radiusScaler;
            _rightOffsetScaler = rightOffsetScaler;
            _upOffsetScaler = upOffsetScaler;
            _lookOffsetScaler = lookOffsetScaler;
            _frictionMultiplier = frictionMultiplier;
        }

        public void UpdateRigidbodyMass(float combinedMultiplier, float massValue, float softness)
        {
            float rbMass = combinedMultiplier * _baseRbMassScaler.Scale(DEFAULT_MASS, massValue, softness);
            left.UpdateRigidbodyMass(rbMass);
            right.UpdateRigidbodyMass(rbMass);
        }

        public void UpdateDimensions(float massValue, float softness)
        {
            float radius = -_radiusScaler.Scale(radiusJsf.val, massValue, softness);
            left.UpdateDimensions(radius);
            right.UpdateDimensions(radius);
        }

        public void UpdatePosition(float massValue, float softness)
        {
            float rightOffset = _rightOffsetScaler.Scale(rightJsf.val, massValue, softness);
            float upOffset = -_upOffsetScaler.Scale(upJsf.val, massValue, softness);
            float lookOffset = -_lookOffsetScaler.Scale(lookJsf.val, massValue, softness);
            left.UpdatePosition(-rightOffset, upOffset, lookOffset);
            right.UpdatePosition(rightOffset, upOffset, lookOffset);
        }

        public void UpdateMaxFrictionalDistance(float sizeMultiplierLeft, float sizeMultiplierRight, float smallestRadius)
        {
            float halfSqrSmallestRadiusMillimeters = 0.5f * smallestRadius * smallestRadius * 1000;
            left.maxFrictionalDistance = sizeMultiplierLeft * halfSqrSmallestRadiusMillimeters;
            right.maxFrictionalDistance = sizeMultiplierRight * halfSqrSmallestRadiusMillimeters;
        }

        public void UpdateFriction(float max)
        {
            left.UpdateFriction(_frictionMultiplier, max);
            right.UpdateFriction(_frictionMultiplier, max);
        }

        public void AutoColliderSizeSet()
        {
            left.AutoColliderSizeSet();
            right.AutoColliderSizeSet();
        }

        public void Calibrate(Vector3 breastCenterLeft, Vector3 breastCenterRight, Rigidbody chestRb)
        {
            left.Calibrate(breastCenterLeft, chestRb);
            right.Calibrate(breastCenterRight, chestRb);
        }

        public void UpdateDistanceDiffs(Vector3 breastCenterLeft, Vector3 breastCenterRight, Rigidbody chestRb)
        {
            left.UpdateDistanceDiff(breastCenterLeft, chestRb);
            right.UpdateDistanceDiff(breastCenterRight, chestRb);
        }

        public void ResetDistanceDiffs()
        {
            left.ResetDistanceDiff();
            right.ResetDistanceDiff();
        }

        public void EnableMultiplyFriction()
        {
            left.EnableMultiplyFriction();
            right.EnableMultiplyFriction();
        }

        public void RestoreDefaults()
        {
            left.RestoreDefaults(DEFAULT_MASS);
            right.RestoreDefaults(DEFAULT_MASS);
        }

        public bool HasRigidbodies() => left.collider.attachedRigidbody != null && right.collider.attachedRigidbody != null;
    }
}
