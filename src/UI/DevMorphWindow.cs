using System.Collections.Generic;
using System.Linq;
using TittyMagic.Handlers;
using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class DevMorphWindow : WindowBase, IWindow
    {
        private readonly UnityAction _onReturnToParent;
        private readonly Dictionary<string, UIDynamic> _sectionElements;

        public DevMorphWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            _onReturnToParent = onReturnToParent;
            _sectionElements = new Dictionary<string, UIDynamic>();
        }

        private static string DirKey(string value) =>
            value.EndsWith("_C") || value.StartsWith("LT") || value.StartsWith("RT")
                ? value
                : $"{value}_L";

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);

            /* Direction chooser */
            {
                var storable = ForceMorphHandler.directionChooser;
                var chooser = tittyMagic.CreatePopupAuto(storable, true, 360f);
                chooser.popup.labelText.color = Color.black;
                elements[storable.name] = chooser;

                chooser.popup.onValueChangeHandlers += RebuildSection;
            }

            {
                var button = tittyMagic.CreateButton("Print");
                button.button.onClick.AddListener(() =>
                {
                    var configs = ForceMorphHandler.configSets[DirKey(ForceMorphHandler.directionChooser.val)];
                    string[] arr = configs.Select(config => config.ToCodeString()).ToArray();
                    Debug.Log(string.Join("", arr));
                });
                button.height = 55f;
                elements["printButton"] = button;
            }

            CreateHeaderTextField(new JSONStorableString("softMultiplierHeader", "Softness Multiplier"));
            CreateHeaderTextField(new JSONStorableString("massMultiplierHeader", "Mass Multiplier"), true);

            RebuildSection(ForceMorphHandler.directionChooser.val);
        }

        private void RebuildSection(string value)
        {
            ClearSection();
            var leftConfigs = ForceMorphHandler.configSets[DirKey(value)];
            foreach(var config in leftConfigs)
            {
                /* Create softness multiplier slider */
                {
                    var storable = config.softMultiplierJsf;
                    var slider = tittyMagic.CreateSlider(storable);
                    slider.valueFormat = "F2";
                    slider.label = config.Label();
                    _sectionElements[$"{slider.label} {storable.name}"] = slider;
                }

                /* Create mass multiplier sliders */
                {
                    var storable = config.massMultiplierJsf;
                    var slider = tittyMagic.CreateSlider(storable, true);
                    slider.valueFormat = "F2";
                    slider.label = config.Label();
                    _sectionElements[$"{slider.label} {storable.name}"] = slider;
                }
            }
        }

        public new void Clear()
        {
            base.Clear();
            ClearSection();

            /* Remove direction popup change handler */
            {
                var element = elements[ForceMorphHandler.directionChooser.name];
                var uiDynamicPopup = element as UIDynamicPopup;
                if(uiDynamicPopup != null)
                {
                    uiDynamicPopup.popup.visible = false;
                    uiDynamicPopup.popup.onValueChangeHandlers -= RebuildSection;
                }
            }
        }

        private void ClearSection()
        {
            _sectionElements
                .ToList()
                .ForEach(element => tittyMagic.RemoveElement(element.Value));
            _sectionElements.Clear();
        }
    }
}
