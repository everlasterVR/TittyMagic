using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class PhysicsWindow : IWindow
    {
        private readonly Script _script;
        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, UIDynamic> elements { get; private set; }
        private Dictionary<string, ParameterWindow> _parameterWindows;
        private string _activeParamWindowKey;

        private readonly JSONStorableString _jointPhysicsParamsHeader;
        private readonly JSONStorableString _softPhysicsParamsHeader;

        public int Id() => 2;

        public PhysicsWindow(Script script)
        {
            _script = script;

            _jointPhysicsParamsHeader = new JSONStorableString("mainPhysicsParamsHeader", "");
            if(Gender.isFemale) _softPhysicsParamsHeader = new JSONStorableString("softPhysicsParamsHeader", "");
            CreateParameterWindows();
        }

        private void CreateParameterWindows()
        {
            _parameterWindows = new Dictionary<string, ParameterWindow>();

            _script.mainPhysicsHandler.leftBreastParameters.Keys.ToList()
                .ForEach(key =>
                {
                    _parameterWindows[key] = new ParameterWindow(
                        _script,
                        _script.mainPhysicsHandler.leftBreastParameters[key],
                        _script.mainPhysicsHandler.rightBreastParameters[key]
                    );
                });
            if(Gender.isFemale)
            {
                _script.softPhysicsHandler.leftBreastParameters.Keys.ToList()
                    .ForEach(key =>
                    {
                        _parameterWindows[key] = new ParameterWindow(
                            _script,
                            _script.softPhysicsHandler.leftBreastParameters[key],
                            _script.softPhysicsHandler.rightBreastParameters[key]
                        );
                    });
            }
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateHeader(_jointPhysicsParamsHeader, "Joint Physics Parameters", false);
            _script.mainPhysicsHandler?.leftBreastParameters.ToList()
                .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, false));

            if(Gender.isFemale)
            {
                CreateHeader(_softPhysicsParamsHeader, "Soft Physics Parameters", true);
                CreateAllowSelfCollisionToggle(true);
                _script.softPhysicsHandler.leftBreastParameters.ToList()
                    .ForEach(kvp => CreateParamButton(kvp.Key, kvp.Value, true));
            }
        }

        private void CreateAllowSelfCollisionToggle(bool rightSide)
        {
            var storable = _script.softPhysicsHandler.allowSelfCollision;
            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Soft Physics Self Collide";
            elements[storable.name] = toggle;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateParamButton(string key, PhysicsParameter param, bool rightSide)
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
                _activeParamWindowKey = key;
                _parameterWindows[key].Rebuild(returnCallback);
                _script.EnableCurrentTabRenavigation();
            });

            elements[key] = button;
        }

        public void Clear()
        {
            if(_activeParamWindowKey != null)
                ClearNestedWindow(_activeParamWindowKey);
            else
                ClearSelf();
        }

        private void ClearSelf()
        {
            foreach(var element in elements)
            {
                _script.RemoveElement(element.Value);
            }
        }

        private void ClearNestedWindow(string key)
        {
            _parameterWindows[key].Clear();
            _activeParamWindowKey = null;
        }
    }
}
