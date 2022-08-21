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
        public string visualizerEditableId => _left.visualizerEditableId;
        public bool syncInProgress { get; set; }

        private readonly Scaler _baseRbMassSliderScaler;
        private readonly Scaler _radiusSliderScaler;
        private readonly Scaler _lengthSliderScaler;
        private readonly Scaler _rightOffsetSliderScaler;
        private readonly Scaler _upOffsetSliderScaler;
        private readonly Scaler _lookOffsetSliderScaler;

        private readonly ColliderConfig _left;
        private readonly ColliderConfig _right;

        public JSONStorableFloat forceJsf { get; set; }
        public JSONStorableFloat radiusJsf { get; set; }
        public JSONStorableFloat lengthJsf { get; set; }
        public JSONStorableFloat rightJsf { get; set; }
        public JSONStorableFloat upJsf { get; set; }
        public JSONStorableFloat lookJsf { get; set; }

        public ColliderConfigGroup(
            string id,
            ColliderConfig left,
            ColliderConfig right,
            Scaler baseRbMassSliderScaler,
            Scaler radiusSliderScaler,
            Scaler lengthSliderScaler,
            Scaler rightOffsetSliderScaler,
            Scaler upOffsetSliderScaler,
            Scaler lookOffsetSliderScaler
        )
        {
            this.id = id;
            _left = left;
            _right = right;
            _baseRbMassSliderScaler = baseRbMassSliderScaler;
            _radiusSliderScaler = radiusSliderScaler;
            _lengthSliderScaler = lengthSliderScaler;
            _rightOffsetSliderScaler = rightOffsetSliderScaler;
            _upOffsetSliderScaler = upOffsetSliderScaler;
            _lookOffsetSliderScaler = lookOffsetSliderScaler;
        }

        public void UpdateRigidbodyMass(float combinedMultiplier, float massValue, float softness)
        {
            float rbMass = combinedMultiplier * _baseRbMassSliderScaler.Scale(DEFAULT_MASS, massValue, softness);
            _left.UpdateRigidbodyMass(rbMass);
            _right.UpdateRigidbodyMass(rbMass);
        }

        public void RestoreDefaultMass()
        {
            _left.UpdateRigidbodyMass(DEFAULT_MASS);
            _right.UpdateRigidbodyMass(DEFAULT_MASS);
        }

        public void UpdateDimensions(float massValue, float softness)
        {
            float radius = -_radiusSliderScaler.Scale(radiusJsf.val, massValue, softness);
            float length = -_lengthSliderScaler.Scale(lengthJsf.val, massValue, softness);
            _left.UpdateDimensions(radius, length);
            _right.UpdateDimensions(radius, length);
        }

        public void UpdatePosition(float massValue, float softness)
        {
            float rightOffset = _rightOffsetSliderScaler.Scale(rightJsf.val, massValue, softness);
            float upOffset = -_upOffsetSliderScaler.Scale(upJsf.val, massValue, softness);
            float lookOffset = -_lookOffsetSliderScaler.Scale(lookJsf.val, massValue, softness);
            _left.UpdatePosition(-rightOffset, upOffset, lookOffset);
            _right.UpdatePosition(rightOffset, upOffset, lookOffset);
        }

        public void AutoColliderSizeSet()
        {
            _left.AutoColliderSizeSet();
            _right.AutoColliderSizeSet();
        }

        public void RestoreDefaults()
        {
            _left.RestoreDefaults();
            _right.RestoreDefaults();
        }

        public bool HasRigidbodies() => _left.HasRigidbody() && _right.HasRigidbody();
    }

    internal class ColliderConfig
    {
        private readonly AutoCollider _autoCollider;
        private readonly Collider _collider;

        public string visualizerEditableId { get; }

        public ColliderConfig(AutoCollider autoCollider, string visualizerEditableId)
        {
            _autoCollider = autoCollider;
            _autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;

            _collider = _autoCollider.jointCollider;
            _collider.enabled = true;
            _collider.GetComponent<CapsuleLineSphereCollider>().enabled = true;

            this.visualizerEditableId = visualizerEditableId;
        }

        public void UpdateRigidbodyMass(float mass) =>
            _collider.attachedRigidbody.mass = mass;

        public void UpdateDimensions(float radiusOffset, float lengthOffset)
        {
            _autoCollider.autoRadiusBuffer = radiusOffset;
            _autoCollider.autoLengthBuffer = lengthOffset + 2 * radiusOffset;
        }

        public void UpdatePosition(float rightOffset, float upOffset, float lookOffset)
        {
            _autoCollider.colliderRightOffset = rightOffset;
            _autoCollider.colliderUpOffset = upOffset;
            _autoCollider.colliderLookOffset = lookOffset;
        }

        public void RestoreDefaults()
        {
            _autoCollider.autoRadiusBuffer = 0;
            _autoCollider.autoLengthBuffer = 0;
            _autoCollider.colliderRightOffset = 0;
            _autoCollider.colliderUpOffset = 0;
            _autoCollider.colliderLookOffset = 0;
            _autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
            _autoCollider.AutoColliderSizeSet(true);
            _autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.MorphChangeOnly;
        }

        public void AutoColliderSizeSet()
        {
            _autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
            _autoCollider.AutoColliderSizeSet(true);
            _autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;
        }

        public bool HasRigidbody() => _collider.attachedRigidbody != null;
    }
}
