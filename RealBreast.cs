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

        private DAZMorph breastsNatural;
        private DAZMorph breastsTogetherApart;
        private DAZMorph breastsDepth;
        private DAZMorph breastsPushUp01;
        private DAZMorph breastsHangForward;
        private DAZMorph breastsHeight;
        private DAZMorph breastsWidth;
        private DAZMorph breastsFlatten;
        private DAZMorph breastRotateXOutRight;
        private DAZMorph breastRotateXOutLeft;
        private DAZMorph breastSag1;
        private DAZMorph breastSag2;
        private DAZMorph breastsSize;
        private DAZMorph breastMoveUpRight;
        private DAZMorph breastMoveUpLeft;
        private DAZMorph breastSideSmoother;
        private DAZMorph breastUnderSmoother1;
        private DAZMorph breastUnderSmoother3;
        private DAZMorph centerGapSmooth;
        private DAZMorph centreGapNarrow;
        private DAZMorph centreGapWide;
        private DAZMorph breastRotateXInRight;
        private DAZMorph breastRotateXInLeft;
        private DAZMorph breastMoveS2SInRight;
        private DAZMorph breastMoveS2SInLeft;
        private DAZMorph breastMoveS2SOutRight;
        private DAZMorph breastMoveS2SOutLeft;

        //storables
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
            breastsNatural = morphUI.GetMorphByDisplayName("Breasts Natural (Pose)");
            breastsTogetherApart = morphUI.GetMorphByDisplayName("Breasts TogetherApart");
            // lean forward
            breastsDepth = morphUI.GetMorphByDisplayName("Breasts Depth");
            breastsPushUp01 = morphUI.GetMorphByDisplayName("Breasts Push Up 01 (Pose)");
            breastsHangForward = morphUI.GetMorphByDisplayName("Breasts Hang Forward");
            breastsHeight = morphUI.GetMorphByDisplayName("Breasts Height");
            breastsWidth = morphUI.GetMorphByDisplayName("Breasts Width");
            // lean back
            breastsFlatten = morphUI.GetMorphByDisplayName("Breasts Flatten");
            breastRotateXOutRight = morphUI.GetMorphByDisplayName("Breast Rotate X Out Right");
            breastRotateXOutLeft = morphUI.GetMorphByDisplayName("Breast Rotate X Out Left");
            // upside down
            breastsSize = morphUI.GetMorphByDisplayName("Breasts Size (Pose)");
            breastMoveUpRight = morphUI.GetMorphByDisplayName("Breast Move Up Right");
            breastMoveUpLeft = morphUI.GetMorphByDisplayName("Breast Move Up Left");
            breastSag1 = morphUI.GetMorphByDisplayName("Breast Sag1 (Pose)");
            breastSag2 = morphUI.GetMorphByDisplayName("Breast Sag2 (Pose)");
            breastSideSmoother = morphUI.GetMorphByDisplayName("Breast Side Smoother (Pose)");
            breastUnderSmoother1 = morphUI.GetMorphByDisplayName("Breast Under Smoother1 (Pose)");
            breastUnderSmoother3 = morphUI.GetMorphByDisplayName("Breast Under Smoother3 (Pose)");
            // roll
            centerGapSmooth = morphUI.GetMorphByDisplayName("Center Gap Smooth (Pose)");
            centreGapNarrow = morphUI.GetMorphByDisplayName("Centre Gap Narrow (Pose)");
            centreGapWide = morphUI.GetMorphByDisplayName("Centre Gap Wide (Pose)");
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
            breastsNatural.morphValue = 3 * Remap(90 - pitch, effect) / 4;
            breastsDepth.morphValue = baseVal / 3;
            breastsPushUp01.morphValue = baseVal / 3;
            breastsHangForward.morphValue = 2 * baseVal / 3;
            breastsHeight.morphValue = -2 * baseVal / 3;
            breastsWidth.morphValue = -2 * baseVal / 3;
            centreGapWide.morphValue = baseVal / 2;
            breastsTogetherApart.morphValue = 2 * baseVal / 3;

            if(pitch <= 90)
            {
                breastsSize.morphValue = 0;
                ZeroSagMorphs();
                ZeroUpsideDownMorphs();
            }
            //inverted face down
            else
            {
                breastSag1.morphValue = Remap(180 - pitch, -effect, 0);
                breastSag2.morphValue = 2 * breastSag1.morphValue;

                float baseVal2 = Remap(180 - Mathf.Abs(pitch), 0, 90, effect, 0);
                breastsSize.morphValue = -baseVal2 / 5;
                breastMoveUpRight.morphValue = baseVal2 / 3;
                breastMoveUpLeft.morphValue = baseVal2 / 3;
                breastSideSmoother.morphValue = baseVal2 / 2;
                breastUnderSmoother1.morphValue = baseVal2 / 5;
                breastUnderSmoother3.morphValue = baseVal2 / 5;
            }
        }

        void ZeroLeanForwardMorphs()
        {
            breastsDepth.morphValue = 0;
            breastsPushUp01.morphValue = 0;
            breastsHangForward.morphValue = 0;
            breastsHeight.morphValue = 0;
            breastsWidth.morphValue = 0;
            centreGapWide.morphValue = 0;
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
                breastSag2.morphValue = 2 * breastSag1.morphValue;
            }

            float inVal = pitch > -90 ? Mathf.Abs(pitch) : 180 - Mathf.Abs(pitch);
            float baseVal = Remap(inVal, effect);
            breastsNatural.morphValue = 3 * Remap(90 - Mathf.Abs(pitch), effect) / 4;
            breastsFlatten.morphValue = baseVal / 3;
            breastsDepth.morphValue = -baseVal;
            breastsTogetherApart.morphValue = -2 * baseVal / 3;
            breastRotateXOutRight.morphValue = baseVal / 3;
            breastRotateXOutLeft.morphValue = baseVal / 3;

            if(pitch > -90)
            {
                breastsSize.morphValue = 0;
                ZeroUpsideDownMorphs();
            }
            //inverted face up
            else
            {
                float baseVal2 = Remap(180 - Mathf.Abs(pitch), effect, 0);
                breastsSize.morphValue = -baseVal2 / 5;
                breastMoveUpRight.morphValue = baseVal2 / 3;
                breastMoveUpLeft.morphValue = baseVal2 / 3;
                breastSideSmoother.morphValue = baseVal2 / 2;
                breastUnderSmoother1.morphValue = baseVal2 / 5;
                breastUnderSmoother3.morphValue = baseVal2 / 5;
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
            float baseVal = Remap(roll, rollEffect.val);
            centerGapSmooth.morphValue = 2 * baseVal / 3;
            centreGapNarrow.morphValue = 2 * baseVal / 3 ;
            breastRotateXInRight.morphValue = 2 * baseVal / 3;
            breastMoveS2SInRight.morphValue = baseVal / 3;
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
            float baseVal = Remap(Mathf.Abs(roll), rollEffect.val);
            centerGapSmooth.morphValue = 2 * baseVal / 3;
            centreGapNarrow.morphValue = 2 * baseVal / 3;
            breastRotateXInLeft.morphValue = 2 * baseVal / 3;
            breastMoveS2SInLeft.morphValue = baseVal / 3;
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

        void OnDestroy()
        {
            ZeroLeanBackMorphs();
            ZeroLeanForwardMorphs();
            ZeroRollLeftMorphs();
            ZeroRollRightMorphs();
            ZeroSagMorphs();
            ZeroUpsideDownMorphs();
            breastsNatural.morphValue = 0;
            breastsTogetherApart.morphValue = 0;
            centerGapSmooth.morphValue = 0;
            centreGapNarrow.morphValue = 0;
        }

        void DebugInfo(float pitch, float roll)
        {
            rollAndPitchInfo.SetVal($@"
Pitch: {pitch}
Roll: {roll}");

            morphInfo.SetVal($@"
Natural: {breastsNatural.morphValue}
TogetherApart: {breastsTogetherApart.morphValue}

Depth: {breastsDepth.morphValue}
Push Up 01: {breastsPushUp01.morphValue}
Hang Forward: {breastsHangForward.morphValue}
Height: {breastsHeight.morphValue}
Width: {breastsWidth.morphValue}

Flatten: {breastsFlatten.morphValue}
Rotate X Out Right: {breastRotateXOutRight.morphValue}
Rotate X Out Left: {breastRotateXOutLeft.morphValue}

Sag1: {breastSag1.morphValue}
Sag2: {breastSag2.morphValue}
Size: {breastsSize.morphValue}
Move Up Right: {breastMoveUpRight.morphValue}
Move Up Left: {breastMoveUpLeft.morphValue}
Side Smoother: {breastSideSmoother.morphValue}
Under Smoother1: {breastUnderSmoother1.morphValue}
Under Smoother3: {breastUnderSmoother3.morphValue}

Center Gap Smooth: {centerGapSmooth.morphValue}
Centre Gap Narrow: {centreGapNarrow.morphValue}
Centre Gap Wide: {centreGapWide.morphValue}
Rotate X In Right: {breastRotateXInRight.morphValue}
Rotate X In Left: {breastRotateXInLeft.morphValue}
Move S2S In Right: {breastMoveS2SInRight.morphValue}
Move S2S In Left: {breastMoveS2SInLeft.morphValue}
Move S2S Out Right: {breastMoveS2SOutRight.morphValue}
Move S2S Out Left: {breastMoveS2SOutLeft.morphValue}");
        }
    }
}
