// ReSharper disable MemberCanBePrivate.Global
using System;
using System.Collections.Generic;
using TittyMagic.Extensions;
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

        private readonly JSONStorableString _mainPhysicsParamsTitle;
        private readonly JSONStorableString _softPhysicsParamsTitle;

        public int Id() => 4;

        public AdvancedWindow(Script script, MainPhysicsHandler mainPhysicsHandler, SoftPhysicsHandler softPhysicsHandler)
        {
            _script = script;
            _mainPhysicsHandler = mainPhysicsHandler;
            _softPhysicsHandler = softPhysicsHandler;

            _mainPhysicsParamsTitle = new JSONStorableString("mainPhysicsParamsTitleText", "");
            _softPhysicsParamsTitle = new JSONStorableString("softPhysicsParamsTitleText", "");
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateParamsHeader(_mainPhysicsParamsTitle, "Main physics parameters", false);
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

            CreateParamsHeader(_softPhysicsParamsTitle, "Soft physics parameters", true);
            foreach(var kvp in _mainPhysicsHandler.leftBreastParameters)
            {
                PhysicsParameter param = kvp.Value;
                elements[kvp.Key] = CreateParamButton(param.displayName, true);
            }
        }

        private void CreateParamsHeader(JSONStorableString storable, string text, bool rightSide)
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
