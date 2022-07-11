using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TittyMagic.UI
{
    internal class PhysicsWindow : IWindow
    {
        private readonly Script _script;

        private Dictionary<string, UIDynamic> _elements;
        private readonly Dictionary<string, IWindow> _nestedWindows;

        public IWindow GetActiveNestedWindow() => _activeNestedWindow;
        private IWindow _activeNestedWindow;

        private readonly JSONStorableString _jointPhysicsParamsHeader;
        private readonly JSONStorableString _softPhysicsParamsHeader;

        public int Id() => 2;

        public PhysicsWindow(Script script)
        {
            _script = script;
            _nestedWindows = new Dictionary<string, IWindow>();

            _jointPhysicsParamsHeader = new JSONStorableString("mainPhysicsParamsHeader", "");
            if(Gender.isFemale)
            {
                _softPhysicsParamsHeader = new JSONStorableString("softPhysicsParamsHeader", "");
            }

            CreateParameterWindows();
        }

        private void CreateParameterWindows()
        {
            UnityAction onReturnToParent = () =>
            {
                _activeNestedWindow = null;
                RebuildSelf();
            };

            _script.mainPhysicsHandler.parameterGroups.Keys.ToList()
                .ForEach(key => _nestedWindows[key] = new ParameterWindow(
                    _script,
                    _script.mainPhysicsHandler.parameterGroups[key],
                    onReturnToParent
                ));
            if(Gender.isFemale)
            {
                _script.softPhysicsHandler.parameterGroups.Keys.ToList()
                    .ForEach(key => _nestedWindows[key] = new ParameterWindow(
                        _script,
                        _script.softPhysicsHandler.parameterGroups[key],
                        onReturnToParent
                    ));
            }
        }

        public void Rebuild()
        {
            if(_activeNestedWindow != null)
            {
                _activeNestedWindow.Rebuild();
            }
            else
            {
                RebuildSelf();
            }
        }

        private void RebuildSelf()
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

            button.AddListener(() =>
            {
                ClearSelf();
                _activeNestedWindow = _nestedWindows[key];
                _activeNestedWindow.Rebuild();
            });

            var nestedWindow = (ParameterWindow) _nestedWindows[key];
            nestedWindow.parentButton = button;
            button.label = nestedWindow.ParamButtonLabel();
            _elements[key] = button;
        }

        public List<UIDynamicSlider> GetSliders() => null;

        public void Clear()
        {
            if(_activeNestedWindow != null)
            {
                _activeNestedWindow.Clear();
            }
            else
            {
                ClearSelf();
            }
        }

        public void ClosePopups()
        {
        }

        private void ClearSelf() =>
            _elements.ToList().ForEach(element => _script.RemoveElement(element.Value));
    }
}
