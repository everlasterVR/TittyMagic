using System;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class MainWindow
    {
        private readonly MVRScript _script;

        public UIDynamicToggle autoRefreshToggle;
        public UIDynamicButton refreshButton;
        public UIDynamicSlider massSlider;
        public UIDynamicSlider softnessSlider;
        public UIDynamicSlider quicknessSlider;
        public UIDynamicSlider morphingYSlider;
        public UIDynamicSlider morphingXSlider;
        public UIDynamicSlider morphingZSlider;
        public UIDynamicSlider gravityYSlider;
        public UIDynamicSlider gravityXSlider;
        public UIDynamicSlider gravityZSlider;
        public UIDynamicSlider offsetMorphingSlider;
        public UIDynamicSlider nippleErectionSlider;

        public SliderClickMonitor massSCM;
        public SliderClickMonitor softnessSCM;
        public SliderClickMonitor quicknessSCM;
        public SliderClickMonitor yGravitySCM;
        public SliderClickMonitor xGravitySCM;
        public SliderClickMonitor zGravitySCM;
        public SliderClickMonitor offsetMorphingSCM;

        public MainWindow(MVRScript script)
        {
            _script = script;
        }

        public void CreateTitle(JSONStorableString storable, bool rightSide)
        {
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 46;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
            textField.textColor = funkyCyan;
        }

        public void CreateSectionTitle(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 32;
            textField.height = 100;
            textField.backgroundColor = Color.clear;
        }

        public void CreateInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 390;
        }

        public void CreateSmallInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            var textField = _script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = 28;
            textField.height = 115;
        }

        public void CreateAutoRefreshToggle(JSONStorableBool storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            autoRefreshToggle = _script.CreateToggle(storable, rightSide);
        }

        public void CreateRefreshButton(bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            refreshButton = _script.CreateButton("Calculate breast mass", rightSide);
            refreshButton.height = 60;
        }

        public void CreateMassSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            massSlider = _script.CreateSlider(storable, rightSide);
            massSlider.valueFormat = "F3";
            massSCM = massSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateSoftnessSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            softnessSlider = _script.CreateSlider(storable, rightSide);
            softnessSlider.valueFormat = "0f";
            softnessSlider.slider.wholeNumbers = true;
            softnessSCM = softnessSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateQuicknessSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            quicknessSlider = _script.CreateSlider(storable, rightSide);
            quicknessSlider.valueFormat = "0f";
            quicknessSlider.slider.wholeNumbers = true;
            quicknessSCM = quicknessSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateMorphingYSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            morphingYSlider = _script.CreateSlider(storable, rightSide);
            morphingYSlider.valueFormat = "F2";
        }

        public void CreateMorphingXSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            morphingXSlider = _script.CreateSlider(storable, rightSide);
            morphingXSlider.valueFormat = "F2";
        }

        public void CreateMorphingZSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            morphingZSlider = _script.CreateSlider(storable, rightSide);
            morphingZSlider.valueFormat = "F2";
        }

        public void CreateGravityPhysicsYSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            gravityYSlider = _script.CreateSlider(storable, rightSide);
            gravityYSlider.valueFormat = "F2";
            yGravitySCM = gravityYSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateGravityPhysicsXSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            gravityXSlider = _script.CreateSlider(storable, rightSide);
            gravityXSlider.valueFormat = "F2";
            xGravitySCM = gravityXSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateGravityPhysicsZSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            gravityZSlider = _script.CreateSlider(storable, rightSide);
            gravityZSlider.valueFormat = "F2";
            zGravitySCM = gravityZSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateOffsetMorphingSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            offsetMorphingSlider = _script.CreateSlider(storable, rightSide);
            offsetMorphingSlider.valueFormat = "F2";
            offsetMorphingSCM = offsetMorphingSlider.slider.gameObject.AddComponent<SliderClickMonitor>();
        }

        public void CreateNippleErectionSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            _script.NewSpacer(spacing, rightSide);
            nippleErectionSlider = _script.CreateSlider(storable, rightSide);
            nippleErectionSlider.valueFormat = "F2";
        }

        public bool SliderClickIsDown()
        {
            return (massSCM != null && massSCM.isDown) ||
                (softnessSCM != null && softnessSCM.isDown) ||
                (quicknessSCM != null && quicknessSCM.isDown) ||
                (xGravitySCM != null && xGravitySCM.isDown) ||
                (yGravitySCM != null && yGravitySCM.isDown) ||
                (zGravitySCM != null && zGravitySCM.isDown) ||
                (offsetMorphingSCM != null && offsetMorphingSCM.isDown);
        }
    }
}
