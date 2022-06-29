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
            Collider leftCollider,
            Collider rightCollider,
            float radiusMultiplier,
            float lengthMultiplier,
            float massMultiplier
        )
        {
            this.id = id;
            _left = new ColliderConfig(leftCollider, radiusMultiplier, lengthMultiplier, massMultiplier);
            _right = new ColliderConfig(rightCollider, radiusMultiplier, lengthMultiplier, massMultiplier);
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

        public bool HasRigidbodies()
        {
            return _left.HasRigidbody() && _right.HasRigidbody();
        }

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

        private readonly Collider _collider;
        private readonly CapsuleLineSphereCollider _capsulelineSphereCollider;

        private float _baseRadius;
        private float _baseHeight;
        private float _baseMass;

        public ColliderConfig(Collider collider, float radiusMultiplier, float lengthMultiplier, float massMultiplier)
        {
            _collider = collider;
            _capsulelineSphereCollider = collider.GetComponent<CapsuleLineSphereCollider>();
            SetBaseValues(radiusMultiplier, lengthMultiplier, massMultiplier);
        }

        public void UpdateRadius(float multiplier)
        {
            _capsulelineSphereCollider.capsuleCollider.radius = multiplier * _baseRadius;
        }

        public void UpdateHeight(float multiplier)
        {
            _capsulelineSphereCollider.capsuleCollider.height = multiplier * _baseHeight;
        }

        public void UpdateRigidbodyMass(float multiplier)
        {
            _collider.attachedRigidbody.mass = multiplier * _baseMass;
        }

        public void RestoreDefaultMass()
        {
            _collider.attachedRigidbody.mass = DEFAULT_MASS;
        }

        public bool HasRigidbody()
        {
            return _collider.attachedRigidbody != null;
        }

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
