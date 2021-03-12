//#define SHOW_DEBUG

using UnityEngine;

namespace TittyMagic
{
    internal class PhysicsConfig
    {
        protected readonly JSONStorableFloat setting;
        protected readonly float valMinMS; //value at min mass and min softness
        protected readonly float valMaxM; //value at max mass and min softness
        protected readonly float valMaxS; //value at min mass and max softness

        public PhysicsConfig(JSONStorableFloat setting, float valMinMS, float valMaxM, float valMaxS, float valMaxMS = 0f)
        {
            this.setting = setting;
            this.valMinMS = valMinMS;
            this.valMaxM = valMaxM;
            this.valMaxS = valMaxS;
#if SHOW_DEBUG
            Log.Message($"init {setting.name} min {valMinMS} maxM {valMaxM} maxS {valMaxS} maxMS {valMaxMS}");
#endif
        }

        //input mass and softness normalized to (0,1) range
        public void UpdateVal(float mass, float softness)
        {
            setting.val = Calculate(mass, softness);
        }

        public string GetStatus()
        {
            return Formatting.NameValueString(setting.name, setting.val, padRight: 25) + "\n";
        }

        protected float Calculate(float mass, float softness)
        {
            return Mathf.Lerp(valMinMS, valMaxM, mass) + Mathf.Lerp(valMinMS, valMaxS, softness) - valMinMS;
        }
    }

    internal class NipplePhysicsConfig : PhysicsConfig
    {
        public NipplePhysicsConfig(
            JSONStorableFloat setting,
            float valMinMS,
            float valMaxM,
            float valMaxS,
            float valMaxMS = 0
        ) : base(setting, valMinMS, valMaxM, valMaxS, valMaxMS)
        {
        }

        public void UpdateVal(float mass, float softness, float nippleErection)
        {
            setting.val = 1.25f * nippleErection + Calculate(mass, softness);
        }
    }
}
