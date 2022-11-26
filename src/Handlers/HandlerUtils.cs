using static TittyMagic.Script;

namespace TittyMagic.Handlers
{
    public static class HandlerUtils
    {
        public static string JsfName(string paramName, string side) =>
            $"{paramName}{(side == Side.LEFT ? "" : side)}";

        public static JSONStorableFloat NewBaseValueJsf(string jsfName, JSONStorableFloat valueJsf) =>
            new JSONStorableFloat($"{jsfName}BaseValue", valueJsf.val, valueJsf.min, valueJsf.max);

        public static JSONStorableFloat NewOffsetJsf(string jsfName, JSONStorableFloat valueJsf, bool register) =>
            tittyMagic.NewJSONStorableFloat($"{jsfName}Offset", 0, -valueJsf.max, valueJsf.max, shouldRegister: register);
    }
}
