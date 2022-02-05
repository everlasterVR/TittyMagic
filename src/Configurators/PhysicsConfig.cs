using UnityEngine;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class PhysicsConfig
    {
        private readonly JSONStorableFloat setting;
        private readonly float valMinMS; //value at min mass and min softness
        private readonly float valMaxM; //value at max mass and min softness
        private readonly float valMaxS; //value at min mass and max softness
        public bool DependOnPhysicsRate { get; }

        public PhysicsConfig(JSONStorableFloat setting, float valMinMS, float valMaxM, float valMaxS, bool dependOnPhysicsRate)
        {
            this.setting = setting;
            this.valMinMS = valMinMS;
            this.valMaxM = valMaxM;
            this.valMaxS = valMaxS;
            DependOnPhysicsRate = dependOnPhysicsRate;
        }

        //input mass and softness normalized to (0,1) range
        public void UpdateVal(float mass, float softness, float multiplier = 1, float addend = 0)
        {
            setting.val = multiplier * Calculate(mass, softness) + addend;
        }

        public string GetStatus()
        {
            return NameValueString(setting.name, setting.val, padRight: 25) + "\n";
        }

        protected float Calculate(float mass, float softness)
        {
            return Mathf.Lerp(valMinMS, valMaxM, mass) + Mathf.Lerp(valMinMS, valMaxS, softness) - valMinMS;
        }
    }
}
