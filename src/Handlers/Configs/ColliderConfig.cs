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

        private readonly Scaler _baseRbMassScaler;
        private readonly Scaler _radiusScaler;
        private readonly Scaler _lengthScaler;
        private readonly Scaler _rightOffsetScaler;
        private readonly Scaler _upOffsetScaler;
        private readonly Scaler _lookOffsetScaler;

        private readonly StaticPhysicsConfig _rbMassMultiplierConfig;
        private readonly StaticPhysicsConfig _radiusMultiplierConfig;
        private readonly StaticPhysicsConfig _lengthMultiplierConfig;
        private readonly StaticPhysicsConfig _rightOffsetMultiplierConfig;
        private readonly StaticPhysicsConfig _upOffsetMultiplierConfig;
        private readonly StaticPhysicsConfig _lookOffsetMultiplierConfig;

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
            StaticPhysicsConfig rbMassMultiplierConfig,
            StaticPhysicsConfig radiusMultiplierConfig,
            StaticPhysicsConfig lengthMultiplierConfig,
            StaticPhysicsConfig rightOffsetMultiplierConfig,
            StaticPhysicsConfig upOffsetMultiplierConfig,
            StaticPhysicsConfig lookOffsetMultiplierConfig,
            Scaler baseRbMassScaler,
            Scaler radiusScaler,
            Scaler lengthScaler,
            Scaler rightOffsetScaler,
            Scaler upOffsetScaler,
            Scaler lookOffsetScaler
        )
        {
            this.id = id;
            this.left = left;
            this.right = right;
            _rbMassMultiplierConfig = rbMassMultiplierConfig;
            _radiusMultiplierConfig = radiusMultiplierConfig;
            _lengthMultiplierConfig = lengthMultiplierConfig;
            _rightOffsetMultiplierConfig = rightOffsetMultiplierConfig;
            _upOffsetMultiplierConfig = upOffsetMultiplierConfig;
            _lookOffsetMultiplierConfig = lookOffsetMultiplierConfig;
            _baseRbMassScaler = baseRbMassScaler;
            _radiusScaler = radiusScaler;
            _lengthScaler = lengthScaler;
            _rightOffsetScaler = rightOffsetScaler;
            _upOffsetScaler = upOffsetScaler;
            _lookOffsetScaler = lookOffsetScaler;
        }

        public void UpdateRigidbodyMass(float combinedMultiplier, float massValue, float softness)
        {
            float rbMass = _rbMassMultiplierConfig.Calculate(massValue, softness) * combinedMultiplier * _baseRbMassScaler.Scale(DEFAULT_MASS);
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
            float radius = _radiusMultiplierConfig.Calculate(massValue, softness) * _radiusScaler.Scale(radiusJsf.val);
            left.UpdateRadius(-radius);
            right.UpdateRadius(-radius);
        }

        public void UpdateLength(float massValue, float softness)
        {
            float length = _lengthMultiplierConfig.Calculate(massValue, softness) * _lengthScaler.Scale(lengthJsf.val);
            left.UpdateLength(-length);
            right.UpdateLength(-length);
        }

        public void UpdateRightOffset(float massValue, float softness)
        {
            float rightOffset = _rightOffsetMultiplierConfig.Calculate(massValue, softness) * _rightOffsetScaler.Scale(rightJsf.val);
            left.UpdateRightOffset(-rightOffset);
            right.UpdateRightOffset(rightOffset);
        }

        public void UpdateUpOffset(float massValue, float softness)
        {
            float up = _upOffsetMultiplierConfig.Calculate(massValue, softness) * _upOffsetScaler.Scale(upJsf.val);
            left.UpdateUpOffset(-up);
            right.UpdateUpOffset(-up);
        }

        public void UpdateLookOffset(float massValue, float softness)
        {
            float look = _lookOffsetMultiplierConfig.Calculate(massValue, softness) * _lookOffsetScaler.Scale(lookJsf.val);
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
