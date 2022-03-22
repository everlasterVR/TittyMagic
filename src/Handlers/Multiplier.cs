using UnityEngine.UI;

namespace TittyMagic
{
    public class Multiplier
    {
        public readonly Slider slider;
        public float mainMultiplier { get; private set; }
        public float extraMultiplier { get; set; }
        public float oppositeExtraMultiplier { get; set; }

        public Multiplier(Slider slider, bool nonlinear = false)
        {
            this.slider = slider;
            SetValueChangedCallback(nonlinear);
        }

        private void SetValueChangedCallback(bool nonlinear)
        {
            if(nonlinear)
            {
                slider.onValueChanged.AddListener(
                    value => { mainMultiplier = Calc.QuadraticRegression(value); }
                );
                mainMultiplier = Calc.QuadraticRegression(slider.value);
            }
            else
            {
                slider.onValueChanged.AddListener(
                    value => { mainMultiplier = value; }
                );
                mainMultiplier = slider.value;
            }
        }
    }
}
