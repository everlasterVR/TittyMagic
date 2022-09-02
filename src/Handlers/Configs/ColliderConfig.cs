using GPUTools.Physics.Scripts.Behaviours;
using TittyMagic.Components;
using UnityEngine;

namespace TittyMagic.Configs
{
    internal class ColliderConfigGroup
    {
        // Seems to be a hard coded value in VaM. Hard coding it here avoids
        // having to check for attachedRigidbody to be available when calling SetBaseMass.
        private const float DEFAULT_MASS = 0.04f;

        public string id { get; }
        public string visualizerEditableId => left.visualizerEditableId;

        private readonly Scaler _baseRbMassSliderScaler;
        private readonly Scaler _radiusSliderScaler;
        private readonly Scaler _rightOffsetSliderScaler;
        private readonly Scaler _upOffsetSliderScaler;
        private readonly Scaler _lookOffsetSliderScaler;

        private readonly float _frictionMultiplier;

        public ColliderConfig left { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        public ColliderConfig right { get; }

        public JSONStorableFloat forceJsf { get; set; }
        public JSONStorableFloat radiusJsf { get; set; }
        public JSONStorableFloat rightJsf { get; set; }
        public JSONStorableFloat upJsf { get; set; }
        public JSONStorableFloat lookJsf { get; set; }

        public ColliderConfigGroup(
            string id,
            ColliderConfig left,
            ColliderConfig right,
            Scaler baseRbMassSliderScaler,
            Scaler radiusSliderScaler,
            Scaler rightOffsetSliderScaler,
            Scaler upOffsetSliderScaler,
            Scaler lookOffsetSliderScaler,
            float frictionMultiplier
        )
        {
            this.id = id;
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

    internal class ColliderConfig
    {
        public const float DEFAULT_FRICTION = 0.6f;

        public AutoCollider autoCollider { get; }
        public Collider collider { get; }
        public string visualizerEditableId { get; }
        public float maxFrictionalDistance { private get; set; }

        private readonly PhysicMaterial _colliderMaterial;
        private readonly Transform _colliderTransform;
        private float _sqrDistanceDiff;
        private float _sqrNeutralDistance;

        public ColliderConfig(AutoCollider autoCollider, string visualizerEditableId)
        {
            this.autoCollider = autoCollider;
            this.autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;

            collider = this.autoCollider.jointCollider;
            collider.enabled = true;
            collider.GetComponent<CapsuleLineSphereCollider>().enabled = true;
            _colliderMaterial = collider.material;
            _colliderTransform = collider.transform;

            this.visualizerEditableId = visualizerEditableId;
        }

        public void UpdateRigidbodyMass(float mass) => collider.attachedRigidbody.mass = mass;

        public void UpdateDimensions(float radiusOffset)
        {
            autoCollider.autoRadiusBuffer = radiusOffset;
            /* Scale length buffer with radius to ensure that the capsule colliders stay spherical.
             * 2x radius is exact scaling, preserving current center cylinder length.
             * Adding 0.1f ensures cylinder length decreases enough to prevent it
             * having any length. (+buffer => -length)
             */
            autoCollider.autoLengthBuffer = 2f * radiusOffset + 0.1f;
        }

        public void UpdatePosition(float rightOffset, float upOffset, float lookOffset)
        {
            autoCollider.colliderRightOffset = rightOffset;
            autoCollider.colliderUpOffset = upOffset;
            autoCollider.colliderLookOffset = lookOffset;
        }

        public void UpdateFriction(float multiplier, float max)
        {
            float normalizedDistance = Mathf.InverseLerp(0, maxFrictionalDistance, _sqrDistanceDiff);
            float dynamicFriction = max - Mathf.SmoothStep(0, max, normalizedDistance);
            _colliderMaterial.dynamicFriction = multiplier * dynamicFriction;

            float staticMax = 0.1f + max;
            float staticFriction = staticMax - Mathf.SmoothStep(0, staticMax, normalizedDistance);
            _colliderMaterial.staticFriction = multiplier * staticFriction;
        }

        public void AutoColliderSizeSet()
        {
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
            autoCollider.AutoColliderSizeSet();
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;
        }

        public void Calibrate(Vector3 breastCenter, Rigidbody chestRb) =>
            _sqrNeutralDistance = SqrDistanceFromBreastCenter(breastCenter, chestRb);

        public void UpdateDistanceDiff(Vector3 breastCenter, Rigidbody chestRb) =>
            _sqrDistanceDiff = Mathf.Abs(_sqrNeutralDistance - SqrDistanceFromBreastCenter(breastCenter, chestRb));

        private float SqrDistanceFromBreastCenter(Vector3 relativeBreastCenter, Rigidbody chestRb)
        {
            var relativeTransformPosition = Calc.RelativePosition(chestRb, _colliderTransform.position);
            /* Scale up by 1000 (millimeters) */
            return (relativeTransformPosition - relativeBreastCenter).sqrMagnitude * 1000;
        }

        public void ResetDistanceDiff() => _sqrDistanceDiff = 0;

        public void EnableMultiplyFriction() => _colliderMaterial.frictionCombine = PhysicMaterialCombine.Multiply;

        public void RestoreDefaults(float defaultMass)
        {
            UpdateRigidbodyMass(defaultMass);

            autoCollider.autoRadiusBuffer = 0;
            autoCollider.autoLengthBuffer = 0;
            autoCollider.colliderRightOffset = 0;
            autoCollider.colliderUpOffset = 0;
            autoCollider.colliderLookOffset = 0;
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
            autoCollider.AutoColliderSizeSet(true);
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.MorphChangeOnly;

            /* Restore default friction. */
            var material = collider.material;
            material.frictionCombine = PhysicMaterialCombine.Average;
            material.dynamicFriction = DEFAULT_FRICTION;
            material.staticFriction = DEFAULT_FRICTION;
        }
    }
}
