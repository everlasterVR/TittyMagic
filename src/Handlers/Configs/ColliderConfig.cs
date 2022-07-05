using GPUTools.Physics.Scripts.Behaviours;
using UnityEngine;

namespace TittyMagic.Configs
{
    internal class ColliderConfigGroup
    {
        private readonly ColliderConfig _left;
        private readonly ColliderConfig _right;

        // ReSharper disable once MemberCanBePrivate.Global UnusedAutoPropertyAccessor.Global
        public string id { get; }

        public ColliderConfigGroup(
            string id,
            AutoCollider leftAutoCollider,
            AutoCollider rightAutoCollider,
            float radiusMultiplier,
            float lengthMultiplier,
            float massMultiplier
        )
        {
            this.id = id;
            _left = new ColliderConfig(leftAutoCollider, radiusMultiplier, lengthMultiplier, massMultiplier);
            _right = new ColliderConfig(rightAutoCollider, radiusMultiplier, lengthMultiplier, massMultiplier);
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

        public void UpdateScaleOffset(float offset, float radiusMultiplier, float heightMultiplier)
        {
            _left.UpdateScaleOffset(offset, radiusMultiplier, heightMultiplier);
            _right.UpdateScaleOffset(offset, radiusMultiplier, heightMultiplier);
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
        private float _baseMass;

        public ColliderConfig(AutoCollider autoCollider, float radiusMultiplier, float lengthMultiplier, float massMultiplier)
        {
            _autoCollider = autoCollider;
            _collider = _autoCollider.jointCollider;
            _capsulelineSphereCollider = _collider.GetComponent<CapsuleLineSphereCollider>();
            SetBaseValues(radiusMultiplier, lengthMultiplier, massMultiplier);
        }

        public void UpdateRadius(float multiplier) =>
            _capsulelineSphereCollider.capsuleCollider.radius = multiplier * _baseRadius;

        public void UpdateHeight(float multiplier) =>
            _capsulelineSphereCollider.capsuleCollider.height = multiplier * _baseHeight;

        public void UpdateRigidbodyMass(float multiplier) =>
            _collider.attachedRigidbody.mass = multiplier * _baseMass;

        public void UpdateScaleOffset(float offset, float radiusMultiplier, float heightMultiplier)
        {
            var capsule = _capsulelineSphereCollider.capsuleCollider;
            capsule.radius = radiusMultiplier * _baseRadius + offset;
            capsule.height = heightMultiplier * _baseHeight + offset / 2;
        }

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
            _baseRadius = _capsulelineSphereCollider.capsuleCollider.radius * radiusMultiplier;
            _baseHeight = _capsulelineSphereCollider.capsuleCollider.height * heightMultiplier;
            _baseMass = DEFAULT_MASS * massMultiplier;
        }
    }
}