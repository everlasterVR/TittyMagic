using UnityEngine;

namespace TittyMagic
{
    internal class PhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinMS; // value at min mass and min softness
        private readonly float _valMaxM; // value at max mass and min softness
        private readonly float _valMaxS; // value at min mass and max softness
        public bool DependOnPhysicsRate { get; }

        public PhysicsConfig(JSONStorableFloat setting, float valMinMS, float valMaxM, float valMaxS, bool dependOnPhysicsRate)
        {
            _setting = setting;
            _valMinMS = valMinMS;
            _valMaxM = valMaxM;
            _valMaxS = valMaxS;
            DependOnPhysicsRate = dependOnPhysicsRate;
        }

        // input mass and softness normalized to (0,1) range
        public void UpdateVal(float mass, float softness, float multiplier = 1, float addend = 0)
        {
            _setting.val = (multiplier * Calculate(mass, softness)) + addend;
        }

        private float Calculate(float mass, float softness)
        {
            return Mathf.Lerp(_valMinMS, _valMaxM, mass) + Mathf.Lerp(_valMinMS, _valMaxS, softness) - _valMinMS;
        }
    }
}
