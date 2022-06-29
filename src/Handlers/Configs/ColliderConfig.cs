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

        public ColliderConfigGroup(string id, Collider leftCollider, Collider rightCollider, float baseRadiusMultiplier, float baseMassMultiplier)
        {
            this.id = id;
            _left = new ColliderConfig(leftCollider, baseRadiusMultiplier, baseMassMultiplier);
            _right = new ColliderConfig(rightCollider, baseRadiusMultiplier, baseMassMultiplier);
        }

        public void UpdateRadius(float multiplier)
        {
            _left.UpdateRadius(multiplier);
            _right.UpdateRadius(multiplier);
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
        private float _baseMass;

        public ColliderConfig(Collider collider, float baseRadiusMultiplier, float baseMassMultiplier)
        {
            _collider = collider;
            _capsulelineSphereCollider = collider.GetComponent<CapsuleLineSphereCollider>();
            SetBaseValues(baseRadiusMultiplier, baseMassMultiplier);
        }

        public void UpdateRadius(float multiplier)
        {
            _capsulelineSphereCollider.capsuleCollider.radius = multiplier * _baseRadius;
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

        private void SetBaseValues(float baseRadiusMultiplier, float baseMassMultiplier)
        {
            _baseRadius = _capsulelineSphereCollider.capsuleCollider.radius * baseRadiusMultiplier;
            _baseMass = DEFAULT_MASS * baseMassMultiplier;
        }
    }
}
