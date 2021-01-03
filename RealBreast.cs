using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace everlaster
{
    // The basic idea and the following implementations are from BreastAutoGravity v2.0 by VeeRifter
    // https://hub.virtamate.com/resources/breastautogravity.662/
    // - get morphs from containingAtom's morphsControlUI
    // - pitch and roll calculation from chest rotation quarternion
    // - Remap function
    public class RealBreast : MVRScript
    {
        const string pluginName = "RealBreast";
        const string pluginVersion = "1.0.0";
        private Transform chest;

        private DAZMorph breastsTogetherApart;
        private DAZMorph breastsDepth;
        private DAZMorph breastsHangForward;
        private DAZMorph breastsHeight;
        private DAZMorph breastsWidth;
        private DAZMorph breastsFlatten;
        private DAZMorph breastRotateXOutRight;
        private DAZMorph breastRotateXOutLeft;
        private DAZMorph breastSag1; // TODO convert to pose morph
        private DAZMorph breastSag2; // TODO convert to pose morph
        private DAZMorph breastMoveUpRight;
        private DAZMorph breastMoveUpLeft;
        private DAZMorph breastSideSmoother; // TODO convert to pose morph
        private DAZMorph breastUnderSmoother1; // TODO convert to pose morph
        private DAZMorph breastUnderSmoother3; // TODO convert to pose morph
        private DAZMorph centerGapSmooth; // TODO convert to pose morph
        private DAZMorph centreGapNarrow; // TODO convert to pose morph
        private DAZMorph breastRotateXInRight;
        private DAZMorph breastRotateXInLeft;
        private DAZMorph breastMoveS2SInRight;
        private DAZMorph breastMoveS2SInLeft;
        private DAZMorph breastMoveS2SOutRight;
        private DAZMorph breastMoveS2SOutLeft;

        // storables
        protected JSONStorableFloat overallEffect;
        protected JSONStorableFloat pitchEffect;
        protected JSONStorableFloat rollEffect;
        //DebugInfo storables
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
                CreateVersionInfoField();
                InitUI();
                InitMorphs();
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void InitUI()
        {
            overallEffect = CreateFloatSlider("Overall effect", 1f, 0f, 2f);
            pitchEffect = CreateFloatSlider("Pitch effect", 1f, 0f, 2f);
            rollEffect = CreateFloatSlider("Roll effect", 1f, 0f, 2f);

            //DebugInfo fields
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
            JSONStorable js = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector dcs = js as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;

            // lean forward or back
            breastsTogetherApart = morphUI.GetMorphByDisplayName("Breasts TogetherApart");
            // lean forward
            breastsDepth = morphUI.GetMorphByDisplayName("Breasts Depth");
            breastsHangForward = morphUI.GetMorphByDisplayName("Breasts Hang Forward");
            breastsHeight = morphUI.GetMorphByDisplayName("Breasts Height");
            breastsWidth = morphUI.GetMorphByDisplayName("Breasts Width");
            // lean back
            breastsFlatten = morphUI.GetMorphByDisplayName("Breasts Flatten");
            breastRotateXOutRight = morphUI.GetMorphByDisplayName("Breast Rotate X Out Right");
            breastRotateXOutLeft = morphUI.GetMorphByDisplayName("Breast Rotate X Out Left");
            // upside down
            breastMoveUpRight = morphUI.GetMorphByDisplayName("Breast Move Up Right");
            breastMoveUpLeft = morphUI.GetMorphByDisplayName("Breast Move Up Left");
            breastSag1 = morphUI.GetMorphByDisplayName("Breast Sag1");
            breastSag2 = morphUI.GetMorphByDisplayName("Breast Sag2");
            breastSideSmoother = morphUI.GetMorphByDisplayName("Breast Side Smoother");
            breastUnderSmoother1 = morphUI.GetMorphByDisplayName("Breast Under Smoother1");
            breastUnderSmoother3 = morphUI.GetMorphByDisplayName("Breast Under Smoother3");
            // roll
            centerGapSmooth = morphUI.GetMorphByDisplayName("Center Gap Smooth");
            centreGapNarrow = morphUI.GetMorphByDisplayName("Centre Gap Narrow");
            breastRotateXInRight = morphUI.GetMorphByDisplayName("Breast Rotate X In Right");
            breastRotateXInLeft = morphUI.GetMorphByDisplayName("Breast Rotate X In Left");
            breastMoveS2SInRight = morphUI.GetMorphByDisplayName("Breast Move S2S In Right");
            breastMoveS2SInLeft = morphUI.GetMorphByDisplayName("Breast Move S2S In Left");
            breastMoveS2SOutRight = morphUI.GetMorphByDisplayName("Breast Move S2S Out Right");
            breastMoveS2SOutLeft = morphUI.GetMorphByDisplayName("Breast Move S2S Out Left");
        }

        void CreateVersionInfoField()
        {
            JSONStorableString jsonString = new JSONStorableString("VersionInfo", $"{pluginName} {pluginVersion}");
            UIDynamicTextField textField = CreateTextField(jsonString, false);
            textField.UItext.fontSize = 40;
            textField.height = 100;
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

        void ZeroLeanBackMorphs()
        {
            breastsFlatten.morphValue = 0;
            breastRotateXOutRight.morphValue = 0;
            breastRotateXOutLeft.morphValue = 0;
        }

        void ZeroSagMorphs()
        {
            breastSag1.morphValue = 0;
            breastSag2.morphValue = 0;
        }

        void ZeroUpsideDownMorphs()
        {
            breastMoveUpRight.morphValue = 0;
            breastMoveUpLeft.morphValue = 0;
            breastSideSmoother.morphValue = 0;
            breastUnderSmoother1.morphValue = 0;
            breastUnderSmoother3.morphValue = 0;
        }

        void OnLeanForward(float pitch, float effect)
        {
            ZeroLeanBackMorphs();
            float inVal = pitch <= 90 ? pitch : 180 - pitch;
            float baseVal = Remap(inVal, effect);

            breastsDepth.morphValue = baseVal / 2;
            breastsHangForward.morphValue = 2 * baseVal / 3;
            breastsHeight.morphValue = -2 * baseVal / 3;
            breastsWidth.morphValue = -2 * baseVal / 3;
            breastsTogetherApart.morphValue = baseVal / 2;

            if(pitch <= 90)
            {
                ZeroSagMorphs();
                ZeroUpsideDownMorphs();
            }
            //inverted face down
            else
            {
                breastSag1.morphValue = Remap(180 - Mathf.Abs(pitch), -effect, 0);
                breastSag2.morphValue = 3 * breastSag1.morphValue;

                float oneThirdVal = Remap(180 - Mathf.Abs(pitch), 0, 90, effect, 0) / 3;
                breastMoveUpRight.morphValue = oneThirdVal;
                breastMoveUpLeft.morphValue = oneThirdVal;
                breastSideSmoother.morphValue = 2 * oneThirdVal;
                breastUnderSmoother1.morphValue = oneThirdVal;
                breastUnderSmoother3.morphValue = oneThirdVal;
            }
        }

        void ZeroLeanForwardMorphs()
        {
            breastsDepth.morphValue = 0;
            breastsHangForward.morphValue = 0;
            breastsHeight.morphValue = 0;
            breastsWidth.morphValue = 0;
        }

        void OnLeanBack(float pitch, float effect)
        {
            ZeroLeanForwardMorphs();

            if(pitch > -45)
            {
                ZeroSagMorphs();
            }
            else
            {
                breastSag1.morphValue = Remap(180 - Mathf.Abs(pitch), 0, 135, -effect, 0);
                breastSag2.morphValue = 3 * breastSag1.morphValue;
            }

            float inVal = pitch > -90 ? Mathf.Abs(pitch) : 180 - Mathf.Abs(pitch);
            float baseVal = Remap(inVal, effect);

            breastsFlatten.morphValue = baseVal / 4;
            breastsDepth.morphValue = -baseVal / 3;
            breastsTogetherApart.morphValue = -baseVal / 2;
            breastRotateXOutRight.morphValue = baseVal / 3;
            breastRotateXOutLeft.morphValue = baseVal / 3;

            if(pitch > -90)
            {
                ZeroUpsideDownMorphs();
            }
            //inverted face up
            else
            {
                float oneThirdVal = Remap(180 - Mathf.Abs(pitch), effect, 0) / 3;
                breastMoveUpRight.morphValue = oneThirdVal;
                breastMoveUpLeft.morphValue = oneThirdVal;
                breastSideSmoother.morphValue = 2 * oneThirdVal;
                breastUnderSmoother1.morphValue = oneThirdVal;
                breastUnderSmoother3.morphValue = oneThirdVal;
            }
        }

        void ZeroRollRightMorphs()
        {
            breastRotateXInLeft.morphValue = 0;
            breastMoveS2SInLeft.morphValue = 0;
            breastMoveS2SOutRight.morphValue = 0;
        }

        void OnRollLeft(float roll)
        {
            ZeroRollRightMorphs();
            float baseVal = 2 * Remap(roll, rollEffect.val) / 3;

            centerGapSmooth.morphValue = baseVal;
            centreGapNarrow.morphValue = baseVal;
            breastRotateXInRight.morphValue = baseVal;
            breastMoveS2SInRight.morphValue = baseVal;
            breastMoveS2SOutLeft.morphValue = baseVal;
        }

        void ZeroRollLeftMorphs()
        {
            breastRotateXInRight.morphValue = 0;
            breastMoveS2SInRight.morphValue = 0;
            breastMoveS2SOutLeft.morphValue = 0;
        }

        void OnRollRight(float roll)
        {
            ZeroRollLeftMorphs();
            float baseVal = 2 * Remap(Mathf.Abs(roll), rollEffect.val) / 3;

            centerGapSmooth.morphValue = baseVal;
            centreGapNarrow.morphValue = baseVal;
            breastRotateXInLeft.morphValue = baseVal;
            breastMoveS2SInLeft.morphValue = baseVal;
            breastMoveS2SOutRight.morphValue = baseVal;
        }

        float Remap(float inVal, float max2)
        {
            return Remap(inVal, 0, 90, 0, max2);
        }

        float Remap(float inVal, float min2, float max2)
        {
            return Remap(inVal, 0, 90, min2, max2);
        }

        float Remap(float inVal, float min1, float max1, float min2, float max2)
        {
            var ratio = (max2 - min2) / (max1 - min1);
            var c = min2 - ratio * min1;
            return ratio * inVal + c;
        }

        void DebugInfo(float pitch, float roll)
        {
            rollAndPitchInfo.SetVal($@"
Pitch: {pitch}
Roll: {roll}");

            morphInfo.SetVal($@"
TogetherApart: {breastsTogetherApart.morphValue}

Depth: {breastsDepth.morphValue}
Hang Forward: {breastsHangForward.morphValue}
Height: {breastsHeight.morphValue}
Width: {breastsWidth.morphValue}

Flatten: {breastsFlatten.morphValue}
Rotate X Out Right: {breastRotateXOutRight.morphValue}
Rotate X Out Left: {breastRotateXOutLeft.morphValue}

Sag1: {breastSag1.morphValue}
Sag2: {breastSag2.morphValue}
Move Up Right: {breastMoveUpRight.morphValue}
Move Up Left: {breastMoveUpLeft.morphValue}
Side Smoother: {breastSideSmoother.morphValue}
Under Smoother1: {breastUnderSmoother1.morphValue}
Under Smoother3: {breastUnderSmoother3.morphValue}

Center Gap Smooth: {centerGapSmooth.morphValue}
Centre Gap Narrow: {centreGapNarrow.morphValue}
Rotate X In Right: {breastRotateXInRight.morphValue}
Rotate X In Left: {breastRotateXInLeft.morphValue}
Move S2S In Right: {breastMoveS2SInRight.morphValue}
Move S2S In Left: {breastMoveS2SInLeft.morphValue}
Move S2S Out Right: {breastMoveS2SOutRight.morphValue}
Move S2S Out Left: {breastMoveS2SOutLeft.morphValue}");
        }
    }
}
