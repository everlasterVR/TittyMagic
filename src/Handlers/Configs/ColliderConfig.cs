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

        public void UpdateRigidbodyMass(float combinedMultiplier)
        {
            _left.UpdateRigidbodyMass(combinedMultiplier);
            _right.UpdateRigidbodyMass(combinedMultiplier);
        }

        public void RestoreDefaultMass()
        {
            _left.RestoreDefaultMass();
            _right.RestoreDefaultMass();
        }

        public void UpdateRadius()
        {
            _left.UpdateRadius(radiusJsf.val);
            _right.UpdateRadius(radiusJsf.val);
        }

        public void UpdateHeight()
        {
            _left.UpdateHeight(heightJsf.val);
            _right.UpdateHeight(heightJsf.val);
        }

        public void UpdateCenter()
        {
            _left.UpdateCenter(centerXJsf.val, centerYJsf.val, centerZJsf.val);
            _right.UpdateCenter(-centerXJsf.val, centerYJsf.val, centerZJsf.val);
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

        public void SetBaseValues()
        {
            _left.SetBaseValues();
            _right.SetBaseValues();
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

        private readonly float _radiusMultiplier;
        private readonly float _lengthMultiplier;
        private readonly float _massMultiplier;

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
            _radiusMultiplier = radiusMultiplier;
            _lengthMultiplier = lengthMultiplier;
            _massMultiplier = massMultiplier;

            _collider = _autoCollider.jointCollider;
            _capsulelineSphereCollider = _collider.GetComponent<CapsuleLineSphereCollider>();
            SetBaseValues();
            this.visualizerEditableId = visualizerEditableId;
        }

        public void UpdateRigidbodyMass(float multiplier) =>
            _collider.attachedRigidbody.mass = multiplier * _baseMass;

        public void RestoreDefaultMass() => _collider.attachedRigidbody.mass = DEFAULT_MASS;

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

        public void ResetDefaultScale() => _autoCollider.AutoColliderSizeSetFinishFast();

        public bool HasRigidbody() => _collider.attachedRigidbody != null;

        public void SetEnabled(bool value)
        {
            _collider.enabled = value;
            _capsulelineSphereCollider.enabled = value;
        }

        public void SetBaseValues()
        {
            ResetDefaultScale();
            var collider = _capsulelineSphereCollider.capsuleCollider;
            _baseRadius = collider.radius * _radiusMultiplier;
            _baseHeight = collider.height * _lengthMultiplier;
            _baseCenter = collider.center;
            _baseMass = DEFAULT_MASS * _massMultiplier;
        }
    }
}
