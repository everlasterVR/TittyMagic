using System.Collections.Generic;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class AdvancedWindow : IWindow
    {
        private readonly Script _script;
        private readonly MainPhysicsHandler _mainPhysicsHandler;
        private readonly SoftPhysicsHandler _softPhysicsHandler;
        // ReSharper disable once MemberCanBePrivate.Global
        public Dictionary<string, UIDynamic> elements { get; private set; }
        private Dictionary<string, ParameterWindow> _parameterWindows;
        private string _activeParamWindowKey;

        private readonly JSONStorableString _jointPhysicsParamsHeader;
        private readonly JSONStorableString _softPhysicsParamsHeader;

        public int Id() => 4;

        public AdvancedWindow(Script script, MainPhysicsHandler mainPhysicsHandler, SoftPhysicsHandler softPhysicsHandler)
        {
            _script = script;
            _mainPhysicsHandler = mainPhysicsHandler;
            _softPhysicsHandler = softPhysicsHandler;

            _jointPhysicsParamsHeader = new JSONStorableString("mainPhysicsParamsHeader", "");
            _softPhysicsParamsHeader = new JSONStorableString("softPhysicsParamsHeader", "");
            CreateParameterWindows();
        }

        private void CreateParameterWindows()
        {
            _parameterWindows = new Dictionary<string, ParameterWindow>();

            _softPhysicsHandler.leftBreastParameters.Keys.ToList()
                .ForEach(key =>
                {
                    _parameterWindows[key] = new ParameterWindow(
                        _script,
                        _softPhysicsHandler.leftBreastParameters[key],
                        _softPhysicsHandler.rightBreastParameters[key]
                    );
                });
            _softPhysicsHandler.leftNippleParameters.Keys.ToList()
                .ForEach(key =>
                {
                    _parameterWindows[key] = new ParameterWindow(
                        _script,
                        _softPhysicsHandler.leftNippleParameters[key],
                        _softPhysicsHandler.rightNippleParameters[key]
                    );
                });
            _mainPhysicsHandler.leftBreastParameters.Keys.ToList()
                .ForEach(key =>
                {
                    _parameterWindows[key] = new ParameterWindow(
                        _script,
                        _mainPhysicsHandler.leftBreastParameters[key],
                        _mainPhysicsHandler.rightBreastParameters[key]
                    );
                });
        }

        public void Rebuild()
        {
            elements = new Dictionary<string, UIDynamic>();

            CreateSoftPhysicsOnToggle(false, 62);
            CreateAllowSelfCollisionToggle(false);
            CreateUseAuxBreastCollidersToggle(false);

            CreateHeader(_jointPhysicsParamsHeader, "Joint physics parameters", false, spacing: 310);
            foreach(var kvp in _mainPhysicsHandler.leftBreastParameters)
            {
                CreateParamButton(kvp.Key, kvp.Value, false);
            }

            CreateHeader(_softPhysicsParamsHeader, "Soft physics parameters", true);
            foreach(var kvp in _softPhysicsHandler.leftBreastParameters)
            {
                CreateParamButton(kvp.Key, kvp.Value, true);
            }

            foreach(var kvp in _softPhysicsHandler.leftNippleParameters)
            {
                CreateParamButton(kvp.Key, kvp.Value, true);
            }
        }

        private void CreateSoftPhysicsOnToggle(bool rightSide, float spacing)
        {
            var storable = _softPhysicsHandler.softPhysicsOn;

            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);

            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Soft Physics Enabled";
            elements[storable.name] = toggle;
        }

        private void CreateAllowSelfCollisionToggle(bool rightSide)
        {
            var storable = _softPhysicsHandler.allowSelfCollision;
            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Soft Physics Self Collide";
            elements[storable.name] = toggle;
        }

        private void CreateUseAuxBreastCollidersToggle(bool rightSide)
        {
            var storable = _softPhysicsHandler.useAuxBreastColliders;
            var toggle = _script.CreateToggle(storable, rightSide);
            toggle.height = 52;
            toggle.label = "Breast Hard Colliders";
            elements[storable.name] = toggle;
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide, int spacing = 0)
        {
            elements[$"{storable.name}Spacer"] = _script.NewSpacer(spacing, rightSide);
            elements[storable.name] = UIHelpers.HeaderTextField(_script, storable, text, rightSide);
        }

        private void CreateParamButton(string key, PhysicsParameter param, bool rightSide)
        {
            var button = _script.CreateButton(param.displayName, rightSide);
            button.height = 52;

            UnityAction backButtonListener = () =>
            {
                _parameterWindows[key].Clear();
                _activeParamWindowKey = null;
                Rebuild();
            };

            button.AddListener(() =>
            {
                ClearSelf();
                _activeParamWindowKey = key;
                _parameterWindows[key].Rebuild(backButtonListener);
            });

            elements[key] = button;
        }

        public void Clear()
        {
            ClearSelf();
            ClearActiveParamWindow();
        }

        private void ClearSelf()
        {
            foreach(var element in elements)
            {
                _script.RemoveElement(element.Value);
            }
        }

        private void ClearActiveParamWindow()
        {
            if(_activeParamWindowKey != null)
            {
                _parameterWindows[_activeParamWindowKey].Clear();
                _activeParamWindowKey = null;
            }
        }
    }
}
