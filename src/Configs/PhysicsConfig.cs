//#define SHOW_DEBUG

using UnityEngine;

namespace TittyMagic
{
    internal class PhysicsConfig
    {
        private readonly JSONStorableFloat setting;
        private readonly float valMinMS; //value at min mass and min softness
        private readonly float valMaxM; //value at max mass and min softness
        private readonly float valMaxS; //value at min mass and max softness

        public PhysicsConfig(JSONStorableFloat setting, float valMinMS, float valMaxM, float valMaxS, float valMaxMS = 0f)
        {
            this.setting = setting;
            this.valMinMS = valMinMS;
            this.valMaxM = valMaxM;
            this.valMaxS = valMaxS;
#if SHOW_DEBUG
            SuperController.LogMessage($"init {setting.name} min {valMinMS} maxM {valMaxM} maxS {valMaxS} maxMS {valMaxMS}");
#endif
        }

        //input mass and softness normalized to (0,1) range
        public void UpdateVal(float mass, float softness)
        {
            setting.val = Mathf.Lerp(valMinMS, valMaxM, mass) + Mathf.Lerp(valMinMS, valMaxS, softness) - valMinMS;
        }

        public string GetStatus()
        {
            return Formatting.NameValueString(setting.name, setting.val, padRight: 25) + "\n";
        }
    }
}
