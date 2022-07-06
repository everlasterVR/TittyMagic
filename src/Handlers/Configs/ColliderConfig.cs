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
        public string visualizerEditableId => _left.visualizerEditableId;
        public bool syncInProgress { get; set; }

        private readonly float _massMultiplier;
        private readonly float _baseRadiusOffset;
        private readonly float _baseLengthOffset;
        private readonly float _baseRightOffset;
        private readonly float _baseUpOffset;
        private readonly float _baseLookOffset;

        private float baseMass => DEFAULT_MASS * _massMultiplier;

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
            float massMultiplier,
            float baseRadiusOffset,
            float baseLengthOffset,
            float baseRightOffset,
            float baseUpOffset,
            float baseLookOffset
        )
        {
            this.id = id;
            _left = left;
            _right = right;
            _massMultiplier = massMultiplier;
            _baseRadiusOffset = baseRadiusOffset;
            _baseLengthOffset = baseLengthOffset;
            _baseRightOffset = baseRightOffset;
            _baseUpOffset = baseUpOffset;
            _baseLookOffset = baseLookOffset;
        }

        public void UpdateRigidbodyMass(float combinedMultiplier)
        {
            _left.UpdateRigidbodyMass(baseMass * combinedMultiplier);
            _right.UpdateRigidbodyMass(baseMass * combinedMultiplier);
        }

        public void RestoreDefaultMass()
        {
            _left.UpdateRigidbodyMass(DEFAULT_MASS);
            _right.UpdateRigidbodyMass(DEFAULT_MASS);
        }

        public void UpdateRadius()
        {
            float radius = _baseRadiusOffset + radiusJsf.val;
            _left.UpdateRadius(-radius);
            _right.UpdateRadius(-radius);
        }

        public void UpdateLength()
        {
            float length = _baseLengthOffset + lengthJsf.val;
            _left.UpdateLength(-length);
            _right.UpdateLength(-length);
        }

        public void UpdateRightOffset()
        {
            float right = _baseRightOffset + rightJsf.val;
            _left.UpdateRightOffset(-right);
            _right.UpdateRightOffset(right);
        }

        public void UpdateUpOffset()
        {
            float up = _baseUpOffset + upJsf.val;
            _left.UpdateUpOffset(-up);
            _right.UpdateUpOffset(-up);
        }

        public void UpdateLookOffset()
        {
            float look = _baseLookOffset + lookJsf.val;
            _left.UpdateLookOffset(-look);
            _right.UpdateLookOffset(-look);
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

        public void SetEnabled(bool value)
        {
            _left.SetEnabled(value);
            _right.SetEnabled(value);
        }
    }

    internal class ColliderConfig
    {
        private readonly AutoCollider _autoCollider;
        private readonly Collider _collider;
        private readonly CapsuleLineSphereCollider _capsulelineSphereCollider;

        public string visualizerEditableId { get; }

        public ColliderConfig(AutoCollider autoCollider, string visualizerEditableId)
        {
            _autoCollider = autoCollider;
            _autoCollider.resizeTrigger = AutoCollider.ResizeTrigger.None;
            _collider = _autoCollider.jointCollider;
            _capsulelineSphereCollider = _collider.GetComponent<CapsuleLineSphereCollider>();

            this.visualizerEditableId = visualizerEditableId;
        }

        public void UpdateRigidbodyMass(float mass) =>
            _collider.attachedRigidbody.mass = mass;

        public void UpdateRadius(float offset)
        {
            _autoCollider.autoRadiusBuffer = offset;
        }

        public void UpdateLength(float offset)
        {
            _autoCollider.autoLengthBuffer = offset;
        }

        public void UpdateRightOffset(float offset)
        {
            _autoCollider.colliderRightOffset = offset;
        }

        public void UpdateUpOffset(float offset)
        {
            _autoCollider.colliderUpOffset = offset;
        }

        public void UpdateLookOffset(float offset)
        {
            _autoCollider.colliderLookOffset = offset;
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

        public void SetEnabled(bool value)
        {
            _collider.enabled = value;
            _capsulelineSphereCollider.enabled = value;
        }
    }
}
