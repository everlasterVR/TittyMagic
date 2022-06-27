// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TittyMagic.UI.UIHelpers;

namespace TittyMagic.UI
{
    internal class AdvancedWindow : IWindow
    {
        private readonly Script _script;
        private readonly MainPhysicsHandler _mainPhysicsHandler;
        private readonly SoftPhysicsHandler _softPhysicsHandler;
        public Dictionary<string, UIDynamic> elements;

        private readonly JSONStorableString _mainPhysicsParamsHeader;
        private readonly JSONStorableString _softPhysicsParamsHeader;

        public int Id() => 4;

        public AdvancedWindow(Script script, MainPhysicsHandler mainPhysicsHandler, SoftPhysicsHandler softPhysicsHandler)
        {
            _script = script;
            _mainPhysicsHandler = mainPhysicsHandler;
            _softPhysicsHandler = softPhysicsHandler;

            _mainPhysicsParamsHeader = new JSONStorableString("mainPhysicsParamsHeader", "");
            _softPhysicsParamsHeader = new JSONStorableString("softPhysicsParamsHeader", "");
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_mainPhysicsParamsHeader, "Main physics parameters", false);
            foreach(var kvp in _softPhysicsHandler.leftBreastParameters)
            {
                PhysicsParameter param = kvp.Value;
                elements[kvp.Key] = CreateParamButton(param.displayName, false);
            }

            foreach(var kvp in _softPhysicsHandler.leftNippleParameters)
            {
                PhysicsParameter param = kvp.Value;
                elements[kvp.Key] = CreateParamButton(param.displayName, false);
            }

            CreateHeader(_softPhysicsParamsHeader, "Soft physics parameters", true);
            foreach(var kvp in _mainPhysicsHandler.leftBreastParameters)
            {
                PhysicsParameter param = kvp.Value;
                elements[kvp.Key] = CreateParamButton(param.displayName, true);
            }
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide)
        {
            elements[storable.name] = HeaderTextField(_script, storable, text, rightSide);
        }

        private UIDynamicButton CreateParamButton(string label, bool rightSide)
        {
            var button = _script.CreateButton(label, rightSide);
            button.height = 52;
            return button;
        }

        public void Clear()
        {
            foreach(var element in elements)
            {
                _script.RemoveElement(element.Value);
            }
        }
    }
}
