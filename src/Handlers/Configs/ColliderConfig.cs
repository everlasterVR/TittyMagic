using GPUTools.Physics.Scripts.Behaviours;
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
        public bool syncInProgress { get; set; }

        private readonly Scaler _baseRbMassSliderScaler;
        private readonly Scaler _radiusSliderScaler;
        private readonly Scaler _lengthSliderScaler;
        private readonly Scaler _rightOffsetSliderScaler;
        private readonly Scaler _upOffsetSliderScaler;
        private readonly Scaler _lookOffsetSliderScaler;

        public ColliderConfig left { get; }
        public ColliderConfig right { get; }

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
            this.left = left;
            this.right = right;
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
            left.UpdateRigidbodyMass(rbMass);
            right.UpdateRigidbodyMass(rbMass);
        }

        public void RestoreDefaultMass()
        {
            left.UpdateRigidbodyMass(DEFAULT_MASS);
            right.UpdateRigidbodyMass(DEFAULT_MASS);
        }

        public void UpdateRadius(float massValue, float softness)
        {
            float radius = _radiusSliderScaler.Scale(radiusJsf.val, massValue, softness);
            left.UpdateRadius(-radius);
            right.UpdateRadius(-radius);
        }

        public void UpdateLength(float massValue, float softness)
        {
            float length = _lengthSliderScaler.Scale(lengthJsf.val, massValue, softness);
            left.UpdateLength(-length);
            right.UpdateLength(-length);
        }

        public void UpdateRightOffset(float massValue, float softness)
        {
            float rightOffset = _rightOffsetSliderScaler.Scale(rightJsf.val, massValue, softness);
            left.UpdateRightOffset(-rightOffset);
            right.UpdateRightOffset(rightOffset);
        }

        public void UpdateUpOffset(float massValue, float softness)
        {
            float up = _upOffsetSliderScaler.Scale(upJsf.val, massValue, softness);
            left.UpdateUpOffset(-up);
            right.UpdateUpOffset(-up);
        }

        public void UpdateLookOffset(float massValue, float softness)
        {
            float look = _lookOffsetSliderScaler.Scale(lookJsf.val, massValue, softness);
            left.UpdateLookOffset(-look);
            right.UpdateLookOffset(-look);
        }

        public void AutoColliderSizeSet()
        {
            left.AutoColliderSizeSet();
            right.AutoColliderSizeSet();
        }

        public void RestoreDefaults()
        {
            left.RestoreDefaults();
            right.RestoreDefaults();
        }

        public bool HasRigidbodies() => left.HasRigidbody() && right.HasRigidbody();
    }

    internal class ColliderConfig
    {
        public AutoCollider autoCollider { get; }
        public Collider collider { get; }

        public string visualizerEditableId { get; }

        public ColliderConfig(AutoCollider autoCollider, string visualizerEditableId)
        {
            this.autoCollider = autoCollider;
            this.autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;

            collider = this.autoCollider.jointCollider;
            collider.enabled = true;
            collider.GetComponent<CapsuleLineSphereCollider>().enabled = true;

            this.visualizerEditableId = visualizerEditableId;
        }

        public void UpdateRigidbodyMass(float mass) =>
            collider.attachedRigidbody.mass = mass;

        public void UpdateRadius(float offset)
        {
            autoCollider.autoRadiusBuffer = offset;
        }

        public void UpdateLength(float offset)
        {
            autoCollider.autoLengthBuffer = offset;
        }

        public void UpdateRightOffset(float offset)
        {
            autoCollider.colliderRightOffset = offset;
        }

        public void UpdateUpOffset(float offset)
        {
            autoCollider.colliderUpOffset = offset;
        }

        public void UpdateLookOffset(float offset)
        {
            autoCollider.colliderLookOffset = offset;
        }

        public void RestoreDefaults()
        {
            autoCollider.autoRadiusBuffer = 0;
            autoCollider.autoLengthBuffer = 0;
            autoCollider.colliderRightOffset = 0;
            autoCollider.colliderUpOffset = 0;
            autoCollider.colliderLookOffset = 0;
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
            autoCollider.AutoColliderSizeSet(true);
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.MorphChangeOnly;
        }

        public void AutoColliderSizeSet()
        {
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.Always;
            autoCollider.AutoColliderSizeSet(true);
            autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;
        }

        public bool HasRigidbody() => collider.attachedRigidbody != null;
    }
}
