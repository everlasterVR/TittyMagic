using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public static class FrictionCalc
    {
        public static JSONStorableFloat drySkinFriction { get; private set; }
        public static float maxHardColliderFriction { get; private set; }
        public static JSONStorableFloat softColliderFriction { get; private set; }

        private static JSONStorableFloat _glossJsf;
        private static JSONStorableFloat _specularBumpinessJsf;

        public static void Init(JSONStorable skinMaterialsStorable)
        {
            drySkinFriction = tittyMagic.NewJSONStorableFloat("maxHardColliderFriction", 0.750f, 0.000f, 1.000f);
            softColliderFriction = new JSONStorableFloat("softColliderFriction", 1, 0, 1);

            _glossJsf = skinMaterialsStorable.GetFloatJSONParam("Gloss");
            _specularBumpinessJsf = skinMaterialsStorable.GetFloatJSONParam("Specular Bumpiness");

            drySkinFriction.setCallbackFunction = _ => CalculateFriction();
            _glossJsf.setJSONCallbackFunction = _ => CalculateFriction();
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();
        }

        /* Maximum friction that a collider can have, drops off dynamically with distance from collider's normal position */
        public static void CalculateFriction()
        {
            float normalizedLinearGloss = Mathf.InverseLerp(2.000f, 8.000f, _glossJsf.val);
            float normalizedSpecBump = Mathf.InverseLerp(_specularBumpinessJsf.min, _specularBumpinessJsf.max, _specularBumpinessJsf.val);

            /* Gloss based multiplier for how much effect bumpiness has on friction
             * https://www.desmos.com/calculator/ieiybdviqs
             */
            float bumpinessEffectMultiplier = Curves.InverseSmoothStep(normalizedLinearGloss, 1, 0.40f, 0.33f);

            /* Inverse of gloss determines the minimum value for friction along a curve.
             * https://www.desmos.com/calculator/8jwemyzuwr
             */
            float minFriction = drySkinFriction.val * (1 - Curves.InverseSmoothStep(normalizedLinearGloss, 1, 0.56f, 0.86f));

            /* Hard colliders */
            {
                float specBumpComponent = 0.50f * bumpinessEffectMultiplier * normalizedSpecBump;
                maxHardColliderFriction = 0.80f * Mathf.Lerp(minFriction, 1.000f, specBumpComponent);
            }

            /* Soft colliders */
            {
                float specBumpComponent = 0.60f * bumpinessEffectMultiplier * normalizedSpecBump;
                softColliderFriction.val = Mathf.Lerp(minFriction, 1.000f, specBumpComponent);
            }
        }

        public static void RemoveCallbacks()
        {
            _glossJsf.setJSONCallbackFunction = null;
            _specularBumpinessJsf.setJSONCallbackFunction = null;
        }
    }
}
