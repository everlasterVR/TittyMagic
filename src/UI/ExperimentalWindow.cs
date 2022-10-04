using System.Text;
using UnityEngine;
using UnityEngine.Events;
using static TittyMagic.Script;

namespace TittyMagic.UI
{
    public class ExperimentalWindow : WindowBase
    {
        private readonly UnityAction _onReturnToParent;

        public ExperimentalWindow(string id, UnityAction onReturnToParent) : base(id)
        {
            _onReturnToParent = onReturnToParent;
        }

        protected override void OnBuild()
        {
            CreateBackButton(false, _onReturnToParent);

            /* */
            {
                CreateHeaderTextField(new JSONStorableString("smoothingHeader", "Breast Position Smoothing"));
            }

            /* Smoothing info text area */
            {
                var sb = new StringBuilder();
                sb.Append("\n".Size(12));
                sb.Append("<b><i>Smoothing Period</i></b> is the number of physics updates over which the");
                sb.Append(" average breast position is calculated. E.g. a value of 6 with a physics rate of 60 Hz");
                sb.Append(" means that the average is calculated over the last 1 second.");
                sb.Append("\n\n");
                sb.Append("<b><i>Weighting Ratio</i></b> determines whether to emphasize older or newer updates.");
                sb.Append("\n  0 = only the oldest update is used");
                sb.Append("\n  0.5 = all updates are equally important");
                sb.Append("\n  1 = only the newest update is used \n       (smoothing is disabled)");
                var storable = new JSONStorableString("smoothingInfoText", sb.ToString());

                var textField = tittyMagic.CreateTextField(storable);
                textField.UItext.fontSize = 28;
                textField.backgroundColor = Color.clear;
                textField.height = 825;
                elements[storable.name] = textField;
            }

            /* Smoothing period slider */
            {
                var storable = tittyMagic.smoothingPeriodJsf;
                AddSpacer(storable.name, 72 + 65, true);
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F0";
                slider.slider.wholeNumbers = true;
                slider.label = "Smoothing Period";
                elements[storable.name] = slider;
            }

            /* Smoothing weighting ratio slider */
            {
                var storable = tittyMagic.smoothingWeightingRatioJsf;
                var slider = tittyMagic.CreateSlider(storable, true);
                slider.valueFormat = "F2";
                slider.label = "Weighting Ratio";
                elements[storable.name] = slider;
            }
        }
    }
}
