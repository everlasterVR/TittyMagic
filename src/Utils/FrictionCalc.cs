using UnityEngine;

namespace TittyMagic
{
    public static class FrictionCalc
    {
        public static float maxHardColliderFriction { get; private set; }
        public static JSONStorableFloat softColliderFriction { get; private set; }

        private static JSONStorableFloat _glossJsf;
        private static JSONStorableFloat _diffuseBumpinessJsf;
        private static JSONStorableFloat _specularBumpinessJsf;

        public static void Init(JSONStorable skinMaterialsStorable)
        {
            softColliderFriction = new JSONStorableFloat("softColliderFriction", 1, 0, 1);
            _glossJsf = skinMaterialsStorable.GetFloatJSONParam("Gloss");
            _diffuseBumpinessJsf = skinMaterialsStorable.GetFloatJSONParam("Diffuse Bumpiness");
            _specularBumpinessJsf = skinMaterialsStorable.GetFloatJSONParam("Specular Bumpiness");
            _glossJsf.setJSONCallbackFunction = _ => CalculateFriction();
            _diffuseBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();
        }

        /* Maximum friction that a collider can have, drops off dynamically with distance from collider's normal position */
        public static void CalculateFriction()
        {
            float normalizedGloss = Mathf.InverseLerp(5.000f, 8.000f, _glossJsf.val);
            /* Gloss determines a minimum value for max friction along an inverse curve.
             * Max gloss -> friction can be as low as 0 if bumpiness is low
             * Half or below gloss -> friction is at least 0.5 even if bumpiness is low
             */
            float minimumMaxFriction = 0.5f * Curves.InverseSmoothStep(1 - normalizedGloss, 1, 0.37f, 0.20f);

            /* Diffuse bumpiness has a small linear effect */
            float clampedDiffBump = Mathf.Clamp(_diffuseBumpinessJsf.val, 0.250f, 0.750f);

            /* Specular bumpiness affects friction along a curve. */
            float normalizedSpecBump = Curves.InverseSmoothStep(_specularBumpinessJsf.val, _specularBumpinessJsf.max, 0.5f, 0.5f);

            /* Hard colliders */
            {
                /* Weight diffuse bumpiness by inverse of gloss value and specular bumpiness by gloss value */
                float diffBumpComponent = clampedDiffBump * (1 - normalizedGloss);
                float specBumpComponent = 0.75f * normalizedSpecBump * normalizedGloss;
                maxHardColliderFriction = Mathf.Lerp(minimumMaxFriction, 1.000f, diffBumpComponent + specBumpComponent);
            }

            /* Soft colliders */
            {
                /* Soft collider friction increases with diffuse bumpiness even if gloss is at
                 * maximum value, and doesn't increase with specular bumpiness as much.
                 * This is to simulate the effect where a light touch on the skin is slippery
                 * but putting more pressure increases friction (hard collider collision is
                 * triggered).
                 */
                float diffBumpComponent = clampedDiffBump * (1 - Mathf.Lerp(0.000f, 0.750f, normalizedGloss));
                float specBumpComponent = 0.50f * normalizedSpecBump * Mathf.Lerp(0.000f, 0.750f, normalizedGloss);
                float val = Mathf.Lerp(minimumMaxFriction, 1.000f, diffBumpComponent + specBumpComponent);
                softColliderFriction.val = val;
            }
        }

        public static void RemoveCallbacks()
        {
            _glossJsf.setJSONCallbackFunction = null;
            _diffuseBumpinessJsf.setJSONCallbackFunction = null;
            _specularBumpinessJsf.setJSONCallbackFunction = null;
        }
    }
}
