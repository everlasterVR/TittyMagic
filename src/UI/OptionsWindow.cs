using System.Text;
using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class OptionsWindow : WindowBase
    {
        private readonly UnityAction _onReturnToParent;
        private readonly CalibrationHelper _calibrationHelper;

        public OptionsWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            _onReturnToParent = onReturnToParent;
            _calibrationHelper = tittyMagic.calibrationHelper;
        }

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);
            CreateRecalibrateButton(tittyMagic.recalibratePhysics, true);

            /* Header */
            {
                CreateHeaderTextField(new JSONStorableString("calibrationOptionsHeader", "Calibration Options"));
            }

            /* Freeze motion/sound toggle */
            {
                var storable = _calibrationHelper.freezeMotionSoundJsb;
                AddSpacer(storable.name, 5);
                var toggle = tittyMagic.CreateToggle(storable);
                toggle.height = 52;
                toggle.label = "Freeze Motion/Sound";
                elements[storable.name] = toggle;
            }

            /* Freeze motion/sound info text */
            {
                var sb = new StringBuilder();
                sb.Append("Enable the Freeze/Motion Sound toggle (in the below toolbar) during calibration.");
                sb.Append(" This prevents all animation from interfering with the calibration, but also disables audio.");

                var storable = new JSONStorableString("freezeMotionSoundInfoText", sb.ToString());
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 26;
                textField.backgroundColor = Color.clear;
                textField.height = 125;
                elements[storable.name] = textField;
            }

            /* Pause scene animation toggle */
            {
                var storable = _calibrationHelper.pauseSceneAnimationJsb;
                var toggle = tittyMagic.CreateToggle(storable);
                toggle.height = 52;
                toggle.label = "Pause Scene Animation";
                elements[storable.name] = toggle;
            }

            /* Pause scene animation info text */
            {
                var sb = new StringBuilder();
                sb.Append("Pause scene animation during calibration. Warning: this has no effect on other animation");
                sb.Append(" (Animation Pattern, Timeline or other plugin, etc.).");

                var storable = new JSONStorableString("pauseSceneAnimationInfoText", sb.ToString());
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 26;
                textField.backgroundColor = Color.clear;
                textField.height = 125;
                elements[storable.name] = textField;
            }

            /* Disable collision toggle */
            {
                var storable = _calibrationHelper.disableBreastCollisionJsb;
                var toggle = tittyMagic.CreateToggle(storable);
                toggle.height = 52;
                toggle.label = "Disable Breast Collision";
                elements[storable.name] = toggle;
            }

            /* Disable collision info text */
            {
                var sb = new StringBuilder();
                sb.Append("Disable breast collision during calibration. This prevents objects colliding directly");
                sb.Append(" with breasts from interfering with the calibration.");

                var storable = new JSONStorableString("collisionInfoText", sb.ToString());
                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 26;
                textField.backgroundColor = Color.clear;
                textField.height = 150;
                elements[storable.name] = textField;
            }

            /* Auto-update mass toggle */
            {
                var storable = _calibrationHelper.autoUpdateJsb;
                AddSpacer(storable.name, 80, true);
                var toggle = tittyMagic.CreateToggle(storable, true);
                toggle.height = 52;
                toggle.label = "Auto-Update Mass";
                elements[storable.name] = toggle;
            }

            /* Auto-update mass info text */
            {
                var sb = new StringBuilder();
                sb.Append("Calibrate automatically and update breast mass when changes in breast morphs are detected.");
                sb.Append(" Disabling this prevents repeated calibrations due to animation of non-pose morphs (e.g. by other plugins).");

                var storable = new JSONStorableString("autoUpdateInfoText", sb.ToString());
                var textField = tittyMagic.CreateTextField(storable, true);
                textField.UItext.fontSize = 26;
                textField.backgroundColor = Color.clear;
                textField.height = 150;
                elements[storable.name] = textField;
            }

            UpdatePauseSceneAnimationToggleStyle();
        }

        public void UpdatePauseSceneAnimationToggleStyle()
        {
            var storable = _calibrationHelper.pauseSceneAnimationJsb;
            if(!elements.ContainsKey(storable.name))
            {
                return;
            }

            elements[storable.name].SetActiveStyle(!_calibrationHelper.freezeMotionSoundJsb.val, true);
        }
    }
}
