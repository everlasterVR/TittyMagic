using System.Collections.Generic;
using UnityEngine.Events;
using static TittyMagic.UI.UIHelpers;

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

            CreateHeader(_mainPhysicsParamsHeader, "Main physics parameters", false);
            foreach(var kvp in _softPhysicsHandler.leftBreastParameters)
            {
                CreateParamButton(kvp.Key, kvp.Value, false);
            }

            foreach(var kvp in _softPhysicsHandler.leftNippleParameters)
            {
                CreateParamButton(kvp.Key, kvp.Value, false);
            }

            CreateHeader(_softPhysicsParamsHeader, "Soft physics parameters", true);
            foreach(var kvp in _mainPhysicsHandler.leftBreastParameters)
            {
                CreateParamButton(kvp.Key, kvp.Value, true);
            }
        }

        private void CreateHeader(JSONStorableString storable, string text, bool rightSide)
        {
            elements[storable.name] = HeaderTextField(_script, storable, text, rightSide);
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
