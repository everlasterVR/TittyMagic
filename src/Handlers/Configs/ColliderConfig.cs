using GPUTools.Physics.Scripts.Behaviours;
using UnityEngine;

namespace TittyMagic.Configs
{
    internal class ColliderConfigGroup
    {
        public string id { get; }
        public int syncMassStatus { get; set; }

        private readonly ColliderConfig _left;
        private readonly ColliderConfig _right;

        public JSONStorableFloat forceJsf { get; set; }
        public JSONStorableFloat radiusJsf { get; set; }
        public JSONStorableFloat heightJsf { get; set; }
        public JSONStorableFloat centerXJsf { get; set; }
        public JSONStorableFloat centerYJsf { get; set; }
        public JSONStorableFloat centerZJsf { get; set; }

        public string visualizerEditableId => _left.visualizerEditableId;

        public ColliderConfigGroup(string id, ColliderConfig left, ColliderConfig right)
        {
            this.id = id;
            _left = left;
            _right = right;
            syncMassStatus = -1;
        }

        public void UpdateRadius(float multiplier)
        {
            _left.UpdateRadius(multiplier);
            _right.UpdateRadius(multiplier);
        }

        public void UpdateHeight(float multiplier)
        {
            _left.UpdateHeight(multiplier);
            _right.UpdateHeight(multiplier);
        }

        public void UpdateCenter(float xOffset, float yOffset, float zOffset)
        {
            _left.UpdateCenter(xOffset, yOffset, zOffset);
            _right.UpdateCenter(-xOffset, yOffset, zOffset);
        }

        public void UpdateRigidbodyMass(float multiplier)
        {
            _left.UpdateRigidbodyMass(multiplier);
            _right.UpdateRigidbodyMass(multiplier);
        }

        public void RestoreDefaultMass()
        {
            _left.RestoreDefaultMass();
            _right.RestoreDefaultMass();
        }

        public void ResetDefaultScale()
        {
            _left.ResetDefaultScale();
            _right.ResetDefaultScale();
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
        // Seems to be a hard coded value in VaM. Hard coding it here avoids
        // having to check for attachedRigidbody to be available when calling SetBaseValues.
        private const float DEFAULT_MASS = 0.04f;

        private readonly AutoCollider _autoCollider;
        private readonly Collider _collider;
        private readonly CapsuleLineSphereCollider _capsulelineSphereCollider;

        private float _baseRadius;
        private float _baseHeight;
        private Vector3 _baseCenter;
        private float _baseMass;

        public string visualizerEditableId { get; }

        public ColliderConfig(
            AutoCollider autoCollider,
            float radiusMultiplier,
            float lengthMultiplier,
            float massMultiplier,
            string visualizerEditableId
        )
        {
            _autoCollider = autoCollider;
            _collider = _autoCollider.jointCollider;
            _capsulelineSphereCollider = _collider.GetComponent<CapsuleLineSphereCollider>();
            SetBaseValues(radiusMultiplier, lengthMultiplier, massMultiplier);
            this.visualizerEditableId = visualizerEditableId;
        }

        public void UpdateRadius(float multiplier) =>
            _capsulelineSphereCollider.capsuleCollider.radius = multiplier * _baseRadius;

        public void UpdateHeight(float multiplier) =>
            _capsulelineSphereCollider.capsuleCollider.height = multiplier * _baseHeight;

        public void UpdateCenter(float xOffset, float yOffset, float zOffset)
        {
            var center = _capsulelineSphereCollider.capsuleCollider.center;
            center.x = _baseCenter.x + xOffset;
            center.y = _baseCenter.y + yOffset;
            center.z = _baseCenter.z + zOffset;
            _capsulelineSphereCollider.capsuleCollider.center = center;
        }

        public void UpdateRigidbodyMass(float multiplier) =>
            _collider.attachedRigidbody.mass = multiplier * _baseMass;

        public void RestoreDefaultMass() => _collider.attachedRigidbody.mass = DEFAULT_MASS;

        public void ResetDefaultScale() => _autoCollider.AutoColliderSizeSetFinishFast();

        public bool HasRigidbody() => _collider.attachedRigidbody != null;

        public void SetEnabled(bool value)
        {
            _collider.enabled = value;
            _capsulelineSphereCollider.enabled = value;
        }

        private void SetBaseValues(float radiusMultiplier, float heightMultiplier, float massMultiplier)
        {
            var collider = _capsulelineSphereCollider.capsuleCollider;
            _baseRadius = collider.radius * radiusMultiplier;
            _baseHeight = collider.height * heightMultiplier;
            _baseCenter = collider.center;
            _baseMass = DEFAULT_MASS * massMultiplier;
        }
    }
}
