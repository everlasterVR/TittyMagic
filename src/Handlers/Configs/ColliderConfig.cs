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
            ColliderConfig right
        )
        {
            this.id = id;
            _left = left;
            _right = right;
        }

        public void UpdateRigidbodyMass(float combinedMultiplier)
        {
            _left.UpdateRigidbodyMass(combinedMultiplier);
            _right.UpdateRigidbodyMass(combinedMultiplier);
        }

        public void RestoreDefaultMass()
        {
            _left.UpdateRigidbodyMass(DEFAULT_MASS);
            _right.UpdateRigidbodyMass(DEFAULT_MASS);
        }

        public void UpdateRadius()
        {
            _left.UpdateRadius(-radiusJsf.val);
            _right.UpdateRadius(-radiusJsf.val);
        }

        public void UpdateLength()
        {
            _left.UpdateLength(-lengthJsf.val);
            _right.UpdateLength(-lengthJsf.val);
        }

        public void UpdateRightOffset()
        {
            _left.UpdateRightOffset(-rightJsf.val);
            _right.UpdateRightOffset(rightJsf.val);
        }

        public void UpdateUpOffset()
        {
            _left.UpdateUpOffset(upJsf.val);
            _right.UpdateUpOffset(upJsf.val);
        }

        public void UpdateLookOffset()
        {
            _left.UpdateLookOffset(lookJsf.val);
            _right.UpdateLookOffset(lookJsf.val);
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
            AutoColliderSizeSet();
        }

        public void UpdateLength(float offset)
        {
            _autoCollider.autoLengthBuffer = offset;
            AutoColliderSizeSet();
        }

        public void UpdateRightOffset(float offset)
        {
            _autoCollider.colliderRightOffset = offset;
            AutoColliderSizeSet();
        }

        public void UpdateUpOffset(float offset)
        {
            _autoCollider.colliderUpOffset = offset;
            AutoColliderSizeSet();
        }

        public void UpdateLookOffset(float offset)
        {
            _autoCollider.colliderLookOffset = offset;
            AutoColliderSizeSet();
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

        private void AutoColliderSizeSet()
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
