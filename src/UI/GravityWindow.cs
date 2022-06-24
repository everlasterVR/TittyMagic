// ReSharper disable MemberCanBePrivate.Global
using System;
using TittyMagic.Extensions;
using UnityEngine;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class GravityWindow : IWindow
    {
        private readonly Script _script;

        public UIDynamic gravityPhysicsTitleTextFieldSpacer;
        public UIDynamicTextField gravityPhysicsTitleTextField;
        public UIDynamic gravityYSliderSpacer;
        public UIDynamicSlider gravityYSlider;
        public UIDynamic gravityXSliderSpacer;
        public UIDynamicSlider gravityXSlider;
        public UIDynamic gravityZSliderSpacer;
        public UIDynamicSlider gravityZSlider;
        public UIDynamic gravityPhysicsInfoTextFieldSpacer;
        public UIDynamicTextField gravityPhysicsInfoTextField;

        public int Id() => 3;

        public GravityWindow(Script script)
        {
            _script = script;
        }

        public void Rebuild()
        {
            CreateGravityPhysicsTitle(_script.gravityTitleText, false);
            CreateGravityPhysicsYSlider(_script.gravityYStorable, false);
            CreateGravityPhysicsXSlider(_script.gravityXStorable, false);
            CreateGravityPhysicsZSlider(_script.gravityZStorable, false);
            CreateGravityPhysicsMorphingInfoTextArea(_script.gravityInfoText, true, spacing: 100);
        }

        private void CreateGravityPhysicsTitle(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            gravityPhysicsTitleTextFieldSpacer = _script.NewSpacer(spacing, rightSide);
            gravityPhysicsTitleTextField = _script.CreateTextField(storable, rightSide);
            gravityPhysicsTitleTextField.UItext.fontSize = 32;
            gravityPhysicsTitleTextField.height = 100;
            gravityPhysicsTitleTextField.backgroundColor = Color.clear;
        }

        private void CreateGravityPhysicsMorphingInfoTextArea(JSONStorableString storable, bool rightSide, float spacing = 0)
        {
            gravityPhysicsInfoTextFieldSpacer = _script.NewSpacer(spacing, rightSide);
            gravityPhysicsInfoTextField = _script.CreateTextField(storable, rightSide);
            gravityPhysicsInfoTextField.UItext.fontSize = 28;
            gravityPhysicsInfoTextField.height = 390;
        }

        private void CreateGravityPhysicsYSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            gravityYSliderSpacer =_script.NewSpacer(spacing, rightSide);
            gravityYSlider = _script.CreateSlider(storable, rightSide);
            gravityYSlider.valueFormat = "F2";
            gravityYSlider.AddSliderClickMonitor();
        }

        private void CreateGravityPhysicsXSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            gravityXSliderSpacer = _script.NewSpacer(spacing, rightSide);
            gravityXSlider = _script.CreateSlider(storable, rightSide);
            gravityXSlider.valueFormat = "F2";
            gravityXSlider.AddSliderClickMonitor();
        }

        private void CreateGravityPhysicsZSlider(JSONStorableFloat storable, bool rightSide, float spacing = 0)
        {
            gravityZSliderSpacer = _script.NewSpacer(spacing, rightSide);
            gravityZSlider = _script.CreateSlider(storable, rightSide);
            gravityZSlider.valueFormat = "F2";
            gravityZSlider.AddSliderClickMonitor();
        }

        public void Clear()
        {
            _script.RemoveSpacer(gravityPhysicsTitleTextFieldSpacer);
            _script.RemoveTextField(gravityPhysicsTitleTextField);
            _script.RemoveSpacer(gravityYSliderSpacer);
            _script.RemoveSlider(gravityYSlider);
            _script.RemoveSpacer(gravityXSliderSpacer);
            _script.RemoveSlider(gravityXSlider);
            _script.RemoveSpacer(gravityZSliderSpacer);
            _script.RemoveSlider(gravityZSlider);
            _script.RemoveSpacer(gravityPhysicsInfoTextFieldSpacer);
            _script.RemoveTextField(gravityPhysicsInfoTextField);
        }
    }
}
