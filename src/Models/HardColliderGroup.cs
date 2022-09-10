using TittyMagic.Components;
using UnityEngine;

namespace TittyMagic.Models
{
    public class HardColliderGroup
    {
        private const float DEFAULT_MASS = 0.04f; // Seems to be a hard coded value in VaM.

        public string id { get; }
        public string visualizerId { get; }

        private readonly Scaler _baseRbMassSliderScaler;
        private readonly Scaler _radiusSliderScaler;
        private readonly Scaler _rightOffsetSliderScaler;
        private readonly Scaler _upOffsetSliderScaler;
        private readonly Scaler _lookOffsetSliderScaler;

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
            Scaler baseRbMassSliderScaler,
            Scaler radiusSliderScaler,
            Scaler rightOffsetSliderScaler,
            Scaler upOffsetSliderScaler,
            Scaler lookOffsetSliderScaler,
            float frictionMultiplier
        )
        {
            this.id = id;
            this.visualizerId = visualizerId;
            this.left = left;
            this.right = right;
            _baseRbMassSliderScaler = baseRbMassSliderScaler;
            _radiusSliderScaler = radiusSliderScaler;
            _rightOffsetSliderScaler = rightOffsetSliderScaler;
            _upOffsetSliderScaler = upOffsetSliderScaler;
            _lookOffsetSliderScaler = lookOffsetSliderScaler;
            _frictionMultiplier = frictionMultiplier;
        }

        public void UpdateRigidbodyMass(float combinedMultiplier, float massValue, float softness)
        {
            float rbMass = combinedMultiplier * _baseRbMassSliderScaler.Scale(DEFAULT_MASS, massValue, softness);
            left.UpdateRigidbodyMass(rbMass);
            right.UpdateRigidbodyMass(rbMass);
        }

        public void UpdateDimensions(float massValue, float softness)
        {
            float radius = -_radiusSliderScaler.Scale(radiusJsf.val, massValue, softness);
            left.UpdateDimensions(radius);
            right.UpdateDimensions(radius);
        }

        public void UpdatePosition(float massValue, float softness)
        {
            float rightOffset = _rightOffsetSliderScaler.Scale(rightJsf.val, massValue, softness);
            float upOffset = -_upOffsetSliderScaler.Scale(upJsf.val, massValue, softness);
            float lookOffset = -_lookOffsetSliderScaler.Scale(lookJsf.val, massValue, softness);
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
    }
}
