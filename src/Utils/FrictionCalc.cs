using UnityEngine;

namespace TittyMagic
{
    public static class FrictionCalc
    {
        public static float maxFriction { get; private set; }

        private static JSONStorableFloat _glossJsf;
        private static JSONStorableFloat _diffuseBumpinessJsf;
        private static JSONStorableFloat _specularBumpinessJsf;

        public static void Init(JSONStorable skinMaterialsStorable)
        {
            _glossJsf = skinMaterialsStorable.GetFloatJSONParam("Gloss");
            _diffuseBumpinessJsf = skinMaterialsStorable.GetFloatJSONParam("Diffuse Bumpiness");
            _specularBumpinessJsf = skinMaterialsStorable.GetFloatJSONParam("Specular Bumpiness");
            _glossJsf.setJSONCallbackFunction = _ => CalculateMaxFriction();
            _diffuseBumpinessJsf.setJSONCallbackFunction = _ => CalculateMaxFriction();
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateMaxFriction();
        }

        /* Maximum friction that a collider can have, drops off dynamically with distance from collider's normal position */
        public static void CalculateMaxFriction()
        {
            float normalizedGloss = Mathf.InverseLerp(_glossJsf.min, _glossJsf.max, _glossJsf.val);

            /* Gloss determines a minimum value for max friction along an inverse curve.
             * Max gloss -> friction can be as low as 0 if bumpiness is low
             * Min gloss -> friction is at least 0.5 even if bumpiness is low
             */
            float minimumMaxFriction = 0.5f * Curves.InverseSmoothStep(1 - normalizedGloss, 1, 0.37f, 0.20f);

            /* Diffuse bumpiness affects friction linarly up to a bumpiness value of 1, weighted by inverse of gloss. */
            float diffuseBumpinessComponent =
                Mathf.Clamp(_diffuseBumpinessJsf.val, 0, 1.000f) *
                (1 - normalizedGloss);

            /* Specular bumpiness affects friction along a curve up to a bumpiness value of 2, weighted by gloss. */
            float specularBumpinessComponent = 0.75f *
                Curves.InverseSmoothStep(_specularBumpinessJsf.val, _specularBumpinessJsf.max, 0.5f, 0.5f) *
                normalizedGloss;

            float max = Mathf.Lerp(minimumMaxFriction, 1.000f, diffuseBumpinessComponent + specularBumpinessComponent);

            Debug.Log(
                $"normalizedGloss: {normalizedGloss}\n" +
                $"minimumMaxFriction: {minimumMaxFriction}\n" +
                $"diffuseBumpinessComponent: {diffuseBumpinessComponent}\n" +
                $"specularBumpinessComponent: {specularBumpinessComponent}\n" +
                $"max: {max}"
            );

            maxFriction = max;
        }

        public static void RemoveCallbacks()
        {
            _glossJsf.setJSONCallbackFunction = null;
            _diffuseBumpinessJsf.setJSONCallbackFunction = null;
            _specularBumpinessJsf.setJSONCallbackFunction = null;
        }
    }
}
