using System.Text.RegularExpressions;

namespace TittyMagic.Handlers.Configs
{
    public class MorphConfigBase
    {
        public float multiplier { get; }
        public DAZMorph morph { get; }

        public MorphConfigBase(string name, float multiplier)
        {
            this.multiplier = multiplier;
            morph = Utils.GetMorph(name);
        }
    }

    public class MorphConfig
    {
        public DAZMorph morph { get; }
        public bool isNegative { get; }
        public JSONStorableFloat softMultiplierJsf { get; }
        public JSONStorableFloat massMultiplierJsf { get; }
        public float softMultiplier { get; private set; }
        public float massMultiplier { get; private set; }

        public MorphConfig(string name, bool isNegative, JSONStorableFloat softMultiplierJsf, JSONStorableFloat massMultiplierJsf)
        {
            morph = Utils.GetMorph(name);
            this.isNegative = isNegative;
            this.softMultiplierJsf = softMultiplierJsf;
            this.massMultiplierJsf = massMultiplierJsf;

            softMultiplierJsf.setCallbackFunction = val => softMultiplier = val;
            softMultiplier = softMultiplierJsf.val;
            massMultiplierJsf.setCallbackFunction = val => massMultiplier = val;
            massMultiplier = massMultiplierJsf.val;
        }

        private bool isCenterMorph => !morph.displayName.EndsWith(" L") && !morph.displayName.EndsWith(" R");
        private bool isLeftRightMorph => morph.displayName.StartsWith("LT") || morph.displayName.StartsWith("RT");

        public string ToCodeString()
        {
            string directionStr = morph.displayName.Split(' ')[0];
            string dollar = isCenterMorph || isLeftRightMorph ? "" : "$";
            string sideStr = isCenterMorph || isLeftRightMorph ? "" : " {side}";

            return $@"
            new MorphConfig({dollar}""{directionStr}/{Label()}{sideStr}"",
                {$"{isNegative}".ToLower()},
                new JSONStorableFloat(""softMultiplier"", {Calc.RoundToDecimals(softMultiplierJsf.val, 100f):0.00}f, -3.00f, 3.00f),
                new JSONStorableFloat(""massMultiplier"", {Calc.RoundToDecimals(massMultiplierJsf.val, 100f):0.00}f, -3.00f, 3.00f)
            ),";
        }

        public string Label() => isCenterMorph || isLeftRightMorph
            ? morph.displayName
            : Regex.Replace(morph.displayName, @" L$", "");
    }
}
