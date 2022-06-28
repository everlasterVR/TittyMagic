using System;
using GPUTools.Physics.Scripts.Behaviours;
using UnityEngine;

namespace TittyMagic.Configs
{
    internal class ColliderConfig
    {
        private readonly Collider _collider;
        private readonly CapsuleLineSphereCollider _capsulelineSphereCollider;

        // Seems to be a hard coded value in VaM. Hard coding it here avoids having to check for attachedRigidbody to be available.
        private const float ORIGINAL_MASS = 0.04f;

        public float originalRadius { get; set; }
        private float _baseRadius;
        private float _baseMass;

        public ColliderConfig(Collider collider)
        {
            _collider = collider;
            _capsulelineSphereCollider = collider.GetComponent<CapsuleLineSphereCollider>();
        }

        public void UpdateRadius(float multiplier)
        {
            _capsulelineSphereCollider.capsuleCollider.radius = multiplier * _baseRadius;
        }

        public void UpdateRigidbodyMass(float multiplier)
        {
            _collider.attachedRigidbody.mass = multiplier * _baseMass;
        }

        public void ResetRadius()
        {
            _capsulelineSphereCollider.capsuleCollider.radius = originalRadius;
        }

        public void ResetRigidbodyMass()
        {
            // attachedRigidbody is null if hard colliders are disabled
            if(HasRigidbody())
            {
                _collider.attachedRigidbody.mass = ORIGINAL_MASS;
            }
        }

        public bool HasRigidbody()
        {
            return _collider.attachedRigidbody != null;
        }

        public void SetEnabled(bool value, float massMultiplier)
        {
            if(!value)
            {
                ResetRigidbodyMass();
            }

            _collider.enabled = value;
            _capsulelineSphereCollider.enabled = value;

            if(value)
            {
                UpdateRigidbodyMass(massMultiplier);
            }
        }

        public void SetBaseValues(float baseRadiusMultiplier, float baseMassMultiplier)
        {
            originalRadius = _capsulelineSphereCollider.capsuleCollider.radius;
            _baseRadius = originalRadius * baseRadiusMultiplier;
            _baseMass = ORIGINAL_MASS * baseMassMultiplier;
        }
    }
}
