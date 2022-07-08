using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class PhysicsWindow : IWindow
    {
        private readonly Script _script;
        private Dictionary<string, UIDynamic> _elements;

        public Dictionary<string, UIDynamic> GetElements() => _elements;

        public Dictionary<string, ParameterWindow> nestedWindows { get; private set; }
        private string _activeNestedWindowKey;

        private readonly JSONStorableString _jointPhysicsParamsHeader;
        private readonly JSONStorableString _softPhysicsParamsHeader;

        public int Id() => 2;

        public PhysicsWindow(Script script)
        {
            _script = script;

            _jointPhysicsParamsHeader = new JSONStorableString("mainPhysicsParamsHeader", "");
            if(Gender.isFemale)
            {
                _softPhysicsParamsHeader = new JSONStorableString("softPhysicsParamsHeader", "");
            }

            CreateParameterWindows();
        }

        private void CreateParameterWindows()
        {
            nestedWindows = new Dictionary<string, ParameterWindow>();

            _script.mainPhysicsHandler.parameterGroups.Keys.ToList()
                .ForEach(key => nestedWindows[key] = new ParameterWindow(
                    _script,
                    _script.mainPhysicsHandler.parameterGroups[key]
                ));
            if(Gender.isFemale)
            {
                _script.softPhysicsHandler.parameterGroups.Keys.ToList()
                    .ForEach(key => nestedWindows[key] = new ParameterWindow(
                        _script,
                        _script.softPhysicsHandler.parameterGroups[key]
                    ));
            }
        }

        public void Rebuild()
        {
            _elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_jointPhysicsParamsHeader, "Joint Physics Parameters", false);
            _script.mainPhysicsHandler?.parameterGroups.ToList()
                .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, false));

            if(Gender.isFemale)
            {
                CreateHeader(_softPhysicsParamsHeader, "Soft Physics Parameters", true);
                CreateAllowSelfCollisionToggle(true);
                _script.softPhysicsHandler.parameterGroups.ToList()
                    .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, true));
            }
        }

        private void CreateAllowSelfCollisionToggle(bool rightSide)
        {
            var storable = _script.softPhysicsHandler.allowSelfCollision;
            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Soft Physics Self Collide";
            _elements[storable.name] = toggle;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            _elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            _elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateParamButton(string key, PhysicsParameterGroup param, bool rightSide)
        {
            var button = _script.CreateButton("  " + param.displayName, rightSide);
            button.height = 52;
            button.buttonText.alignment = TextAnchor.MiddleLeft;

            UnityAction returnCallback = () =>
            {
                ClearNestedWindow(key);
                Rebuild();
            };

            button.AddListener(() =>
            {
                ClearSelf();
                _activeNestedWindowKey = key;
                nestedWindows[key].Rebuild(returnCallback);
            });

            nestedWindows[key].parentButton = button;
            button.label = nestedWindows[key].ParamButtonLabel();
            _elements[key] = button;
        }

        public void Clear()
        {
            if(_activeNestedWindowKey != null)
            {
                ClearNestedWindow(_activeNestedWindowKey);
            }
            else
            {
                ClearSelf();
            }
        }

        private void ClearSelf() =>
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));

        private void ClearNestedWindow(string key)
        {
            nestedWindows[key].Clear();
            _activeNestedWindowKey = null;
        }

        public void ActionsOnWindowClosed()
        {
        }
    }
}
