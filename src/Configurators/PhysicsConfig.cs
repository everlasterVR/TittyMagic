using UnityEngine;

namespace TittyMagic
{
    internal class BreastPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinMS; // value at min mass and min softness
        private readonly float _valMaxM; // value at max mass and min softness
        private readonly float _valMaxS; // value at min mass and max softness
        public bool dependOnPhysicsRate { get; }

        public BreastPhysicsConfig(JSONStorableFloat setting, float valMinMS, float valMaxM, float valMaxS, bool dependOnPhysicsRate)
        {
            _setting = setting;
            _valMinMS = valMinMS;
            _valMaxM = valMaxM;
            _valMaxS = valMaxS;
            this.dependOnPhysicsRate = dependOnPhysicsRate;
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

    internal class PectoralPhysicsConfig
    {
        private readonly JSONStorableFloat _setting;
        private readonly float _valMinM; // value at min mass
        private readonly float _valMaxM; // value at max mass

        public PectoralPhysicsConfig(JSONStorableFloat setting, float valMinM, float valMaxM)
        {
            _setting = setting;
            _valMinM = valMinM;
            _valMaxM = valMaxM;
        }

        // input mass normalized to (0,1) range
        public void UpdateVal(float mass, float multiplier = 1, float addend = 0)
        {
            _setting.val = (multiplier * Calculate(mass)) + addend;
        }

        private float Calculate(float mass)
        {
            return Mathf.Lerp(_valMinM, _valMaxM, mass);
        }
    }
}
