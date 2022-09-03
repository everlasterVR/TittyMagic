using GPUTools.Physics.Scripts.Behaviours;
using UnityEngine;

namespace TittyMagic.Models
{
    public class HardCollider
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

        public HardCollider(AutoCollider autoCollider, string visualizerEditableId)
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
