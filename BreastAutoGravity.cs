using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace everlaster
{
    public class BreastAutoGravity : MVRScript
    {
        protected string versionText = "BreastAutoGravity v2.0 - VeeRifter";
        private Transform chest;
        private GenerateDAZMorphsControlUI morphUI;

        // lean morphs
        private DAZMorph breastsTogetherApart;
        //      lean forward
        private DAZMorph breastsDepth;
        private DAZMorph breastsHangForward;
        private DAZMorph breastsHeight;
        private DAZMorph breastsWidth;
        //      lean back
        private DAZMorph breastsFlatten;
        private DAZMorph breastRotateXOutRight;
        private DAZMorph breastRotateXOutLeft;
        //      upside down
        private DAZMorph breastSag1; // TODO convert to pose morph
        private DAZMorph breastSag2; // TODO convert to pose morph
        private DAZMorph breastMoveUpRight;
        private DAZMorph breastMoveUpLeft;
        //      lean forward AND upside down
        private DAZMorph breastSideSmoother; // TODO convert to pose morph
        private DAZMorph breastUnderSmoother1; // TODO convert to pose morph
        private DAZMorph breastUnderSmoother3; // TODO convert to pose morph

        // roll morphs
        private DAZMorph breastsMoveS2SDir;
        private DAZMorph breastRotateXInRight;
        private DAZMorph breastRotateXInLeft;
        private DAZMorph breastMoveS2SInRight;
        private DAZMorph breastMoveS2SInLeft;
        private DAZMorph breastMoveS2SOutRight;
        private DAZMorph breastMoveS2SOutLeft;

        // storables
        protected JSONStorableFloat pitchEffect;
        protected JSONStorableFloat rollEffect;
        protected JSONStorableString rollAndPitchInfo = new JSONStorableString("Roll And Pitch Info", "");
        protected JSONStorableString morphInfo = new JSONStorableString("Morph Info", "");

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "Person")
                {
                    SuperController.LogError($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                chest = containingAtom.GetStorableByID("chest").transform;
                InitUI();
                InitMorphs();
                //VersionInfo();
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void InitUI()
        {
            JSONStorable js = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector dcs = js as DAZCharacterSelector;
            morphUI = dcs.morphsControlUI;

            // sliders
            pitchEffect = CreateFloatSlider("Pitch Effect", 1f, 0f, 1f);
            rollEffect = CreateFloatSlider("Roll Effect", 1f, 0f, 1f);

            // debug info
            UIDynamicTextField infoField = CreateTextField(rollAndPitchInfo, false);
            infoField.height = 100;
            UIDynamicTextField infoField2 = CreateTextField(morphInfo, true);
            infoField2.height = 1000;
        }

        JSONStorableFloat CreateFloatSlider(string paramName, float startingValue, float minValue, float maxValue)
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            RegisterFloat(storable);
            CreateSlider(storable, false);
            return storable;
        }

        void InitMorphs()
        {
            // lean morphs
            breastsTogetherApart = morphUI.GetMorphByDisplayName("Breasts TogetherApart");
            //      lean forward
            breastsDepth = morphUI.GetMorphByDisplayName("Breasts Depth");
            breastsHangForward = morphUI.GetMorphByDisplayName("Breasts Hang Forward");
            breastsHeight = morphUI.GetMorphByDisplayName("Breasts Height");
            breastsWidth = morphUI.GetMorphByDisplayName("Breasts Width");
            //      lean back
            breastsFlatten = morphUI.GetMorphByDisplayName("Breasts Flatten");
            breastRotateXOutRight = morphUI.GetMorphByDisplayName("Breast Rotate X Out Right");
            breastRotateXOutLeft = morphUI.GetMorphByDisplayName("Breast Rotate X Out Left");
            //      upside down
            breastMoveUpRight = morphUI.GetMorphByDisplayName("Breast Move Up Right");
            breastMoveUpLeft = morphUI.GetMorphByDisplayName("Breast Move Up Left");
            breastSag1 = morphUI.GetMorphByDisplayName("Breast Sag1");
            breastSag2 = morphUI.GetMorphByDisplayName("Breast Sag2");
            breastSideSmoother = morphUI.GetMorphByDisplayName("Breast Side Smoother");
            breastUnderSmoother1 = morphUI.GetMorphByDisplayName("Breast Under Smoother1");
            breastUnderSmoother3 = morphUI.GetMorphByDisplayName("Breast Under Smoother3");

            // roll morphs
            breastsMoveS2SDir = morphUI.GetMorphByDisplayName("Breasts Move S2S Dir");
            breastRotateXInRight = morphUI.GetMorphByDisplayName("Breast Rotate X In Right");
            breastRotateXInLeft = morphUI.GetMorphByDisplayName("Breast Rotate X In Left");
            breastMoveS2SInRight = morphUI.GetMorphByDisplayName("Breast Move S2S In Right");
            breastMoveS2SInLeft = morphUI.GetMorphByDisplayName("Breast Move S2S In Left");
            breastMoveS2SOutRight = morphUI.GetMorphByDisplayName("Breast Move S2S Out Right");
            breastMoveS2SOutLeft = morphUI.GetMorphByDisplayName("Breast Move S2S Out Left");
        }

        void VersionInfo()
        {
            JSONStorableString jsText = new JSONStorableString("Version", versionText);
            UIDynamicTextField dtf = CreateTextField(jsText, false);
            dtf.height = 115;
        }

        public void Update()
        {
            Quaternion q = chest.rotation;

            float pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
            float roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);

            // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
            // -> if person is sideways, pitch related morphs have less effect
            float scaledPitchEffect = pitchEffect.val * (90 - Mathf.Abs(roll)) / 90;
            if(pitch >= 0)
            {
                OnLeanForward(pitch, scaledPitchEffect);
            }
            else
            {
                OnLeanBack(pitch, scaledPitchEffect);
            }

            if(roll >= 0)
            {
                OnRollLeft(roll);
            }
            else
            {
                OnRollRight(roll);
            }

            DebugInfo(pitch, roll);
        }

        void OnLeanForward(float pitch, float effect)
        {
            breastsFlatten.morphValue = 0;
            breastRotateXOutRight.morphValue = 0;
            breastRotateXOutLeft.morphValue = 0;

            if(pitch <= 90)
            {
                breastSag1.morphValue = 0;
                breastSag2.morphValue = 0;
                breastMoveUpRight.morphValue = 0;
                breastMoveUpLeft.morphValue = 0;
                breastSideSmoother.morphValue = 0;
                breastUnderSmoother1.morphValue = 0;
                breastUnderSmoother3.morphValue = 0;

                float baseVal = Remap(pitch, 0, 90, 0, effect);
                breastsDepth.morphValue = baseVal / 3;
                breastsHangForward.morphValue = baseVal / 2;
                breastsHeight.morphValue = -2 * baseVal / 3;
                breastsWidth.morphValue = -2 * baseVal / 3;
                breastsTogetherApart.morphValue = baseVal / 2;
            }
            //inverted face down
            else
            {

                float baseVal = Remap(180 - pitch, 0, 90, 0, effect);
                breastsDepth.morphValue = baseVal / 3;
                breastsHangForward.morphValue = baseVal / 2;
                breastsHeight.morphValue = -2 * baseVal / 3;
                breastsWidth.morphValue = -2 * baseVal / 3;
                breastsTogetherApart.morphValue = baseVal / 2;

                breastSag2.morphValue = 3 * Remap(180 - Mathf.Abs(pitch), 0, 90, -effect, 0);
                breastSag1.morphValue = breastSag2.morphValue / 3;

                float baseVal2 = Remap(180 - Mathf.Abs(pitch), 0, 90, effect, 0);
                breastMoveUpRight.morphValue = baseVal2 / 3;
                breastMoveUpLeft.morphValue = breastMoveUpRight.morphValue;
                breastSideSmoother.morphValue = 2 * baseVal2 / 3;
                breastUnderSmoother1.morphValue = baseVal2 / 3;
                breastUnderSmoother3.morphValue = baseVal2 / 3;
            }
        }

        void OnLeanBack(float pitch, float effect)
        {
            breastsDepth.morphValue = 0;
            breastsHangForward.morphValue = 0;
            breastsHeight.morphValue = 0;
            breastsWidth.morphValue = 0;

            if(pitch > -45)
            {
                breastSag1.morphValue = 0;
                breastSag2.morphValue = 0;
            }
            else
            {
                breastSag2.morphValue = 3 * Remap(180 - Mathf.Abs(pitch), 0, 135, -effect, 0);
                breastSag1.morphValue = breastSag2.morphValue / 3;
            }

            if(pitch > -90)
            {
                breastMoveUpRight.morphValue = 0;
                breastMoveUpLeft.morphValue = 0;
                breastSideSmoother.morphValue = 0;
                breastUnderSmoother1.morphValue = 0;
                breastUnderSmoother3.morphValue = 0;

                float baseVal = Remap(Mathf.Abs(pitch), 0, 90, 0, effect);
                breastsFlatten.morphValue = baseVal / 4;
                breastsDepth.morphValue = -baseVal / 3;
                breastsTogetherApart.morphValue = -baseVal / 2;
                breastRotateXOutRight.morphValue = baseVal / 3;
                breastRotateXOutLeft.morphValue = breastRotateXOutRight.morphValue;
            }
            //inverted face up
            else
            {
                float baseVal = Remap(180 - Mathf.Abs(pitch), 0, 90, 0, effect);
                breastsFlatten.morphValue = baseVal / 4;
                breastsDepth.morphValue = -baseVal / 3;
                breastsTogetherApart.morphValue = -baseVal / 2;
                breastRotateXOutRight.morphValue = baseVal / 3;
                breastRotateXOutLeft.morphValue = breastRotateXOutRight.morphValue;

                float baseVal2 = Remap(180 - Mathf.Abs(pitch), 0, 90, effect, 0);
                breastMoveUpRight.morphValue = baseVal2 / 3;
                breastMoveUpLeft.morphValue = breastMoveUpRight.morphValue;
                breastSideSmoother.morphValue = 2 * baseVal2 / 3;
                breastUnderSmoother1.morphValue = baseVal2 / 3;
                breastUnderSmoother3.morphValue = baseVal2 / 3;
            }
        }

        void OnRollLeft(float roll)
        {
            breastRotateXInLeft.morphValue = 0;
            breastMoveS2SInLeft.morphValue = 0;
            breastMoveS2SOutRight.morphValue = 0;
            //face up
            if(roll <= 90)
            {
                breastsMoveS2SDir.morphValue = Remap(roll, 0, 90, 0, rollEffect.val);
                breastRotateXInRight.morphValue = Remap(roll, 0, 90, 0, rollEffect.val / 2);
                breastMoveS2SInRight.morphValue = Remap(roll, 0, 90, 0, rollEffect.val / 2);
                breastMoveS2SOutLeft.morphValue = Remap(roll, 0, 90, 0, rollEffect.val / 2);
            }
            //face down
            else
            {
                breastsMoveS2SDir.morphValue = Remap(180 - roll, 0, 90, 0, rollEffect.val);
                breastRotateXInRight.morphValue = Remap(180 - roll, 0, 90, 0, rollEffect.val / 2);
                breastMoveS2SInRight.morphValue = Remap(180 - roll, 0, 90, 0, rollEffect.val / 2);
                breastMoveS2SOutLeft.morphValue = Remap(180 - roll, 0, 90, 0, rollEffect.val / 2);
            }
        }

        void OnRollRight(float roll)
        {
            breastRotateXInRight.morphValue = 0;
            breastMoveS2SInRight.morphValue = 0;
            breastMoveS2SOutLeft.morphValue = 0;
            //face up
            if(roll > -90)
            {
                breastsMoveS2SDir.morphValue = Remap(Mathf.Abs(roll), 0, 90, 0, -rollEffect.val);
                breastRotateXInLeft.morphValue = Remap(-Mathf.Abs(roll), 0, 90, 0, -rollEffect.val / 2);
                breastMoveS2SInLeft.morphValue = Remap(-Mathf.Abs(roll), 0, 90, 0, -rollEffect.val / 2);
                breastMoveS2SOutRight.morphValue = Remap(-Mathf.Abs(roll), 0, 90, 0, -rollEffect.val / 2);
            }
            //face down
            else
            {
                breastsMoveS2SDir.morphValue = Remap(180 - Mathf.Abs(roll), 0, 90, 0, -rollEffect.val);
                breastRotateXInLeft.morphValue = Remap(-180 + Mathf.Abs(roll), 0, 90, 0, -rollEffect.val / 2);
                breastMoveS2SInLeft.morphValue = Remap(-180 + Mathf.Abs(roll), 0, 90, 0, -rollEffect.val / 2);
                breastMoveS2SOutRight.morphValue = Remap(-180 + Mathf.Abs(roll), 0, 90, 0, -rollEffect.val / 2);
            }
        }

        float Remap(float inVal, float min1, float max1, float min2, float max2)
        {
            var ratio = (max2 - min2) / (max1 - min1); // max2 / 90
            var c = min2 - ratio * min1; // 0 when min2 and min1 both 0
            return ratio * inVal + c;
        }

        void DebugInfo(float pitch, float roll)
        {
            string info = $"Pitch: {pitch}";
            info += $"\r\nRoll: {roll}";
            rollAndPitchInfo.SetVal(info);

            string info2 = "";
            info2 += $"\r\nTogetherApart: {breastsTogetherApart.morphValue}";
            info2 += $"\r\n";
            info2 += $"\r\nDepth: {breastsDepth.morphValue}";
            info2 += $"\r\nHang Forward: {breastsHangForward.morphValue}";
            info2 += $"\r\nHeight: {breastsHeight.morphValue}";
            info2 += $"\r\nWidth: {breastsWidth.morphValue}";
            info2 += $"\r\n";
            info2 += $"\r\nFlatten: {breastsFlatten.morphValue}";
            info2 += $"\r\nRotate X Out Right: {breastRotateXOutRight.morphValue}";
            info2 += $"\r\nRotate X Out Left: {breastRotateXOutLeft.morphValue}";
            info2 += $"\r\n";
            info2 += $"\r\nSag1: {breastSag1.morphValue}";
            info2 += $"\r\nSag2: {breastSag2.morphValue}";
            info2 += $"\r\nMove Up Right: {breastMoveUpRight.morphValue}";
            info2 += $"\r\nMove Up Left: {breastMoveUpLeft.morphValue}";
            info2 += $"\r\nSide Smoother: {breastSideSmoother.morphValue}";
            info2 += $"\r\nUnder Smoother1: {breastUnderSmoother1.morphValue}";
            info2 += $"\r\nUnder Smoother3: {breastUnderSmoother3.morphValue}";
            info2 += $"\r\n";
            info2 += $"\r\nCenter Shift: {breastsMoveS2SDir.morphValue}";
            info2 += $"\r\nMove S2S Dir: {breastsMoveS2SDir.morphValue}";
            info2 += $"\r\nRotate X In Right: {breastRotateXInRight.morphValue}";
            info2 += $"\r\nRotate X In Left: {breastRotateXInLeft.morphValue}";
            info2 += $"\r\nMove S2S In Right: {breastMoveS2SInRight.morphValue}";
            info2 += $"\r\nMove S2S In Left: {breastMoveS2SInLeft.morphValue}";
            info2 += $"\r\nMove S2S Out Right: {breastMoveS2SOutRight.morphValue}";
            info2 += $"\r\nMove S2S Out Left: {breastMoveS2SOutLeft.morphValue}";
            morphInfo.SetVal(info2);
        }
    }
}
