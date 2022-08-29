using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public static class FrictionCalc
    {
        public static JSONStorable skinMaterials { get; private set; }
        public static JSONStorableBool enableAdaptiveFriction { get; private set; }
        public static JSONStorableFloat drySkinFriction { get; private set; }
        public static float maxHardColliderFriction { get; private set; }
        public static JSONStorableFloat softColliderFriction { get; private set; }

        private static JSONStorableFloat _glossJsf;
        private static JSONStorableFloat _specularBumpinessJsf;

        public static void Init(JSONStorable skinMaterialsStorable)
        {
            skinMaterials = skinMaterialsStorable;

            enableAdaptiveFriction = tittyMagic.NewJSONStorableBool("enableAdaptiveFriction", false);
            enableAdaptiveFriction.setCallbackFunction = val =>
            {
                if(tittyMagic.enableAdaptiveFrictionToggle != null)
                {
                    tittyMagic.enableAdaptiveFrictionToggle.textColor = val ? UIHelpers.funkyCyan : Color.white;
                }

                CalculateFriction();
            };

            drySkinFriction = tittyMagic.NewJSONStorableFloat("drySkinFriction", 0.750f, 0.000f, 1.000f);
            drySkinFriction.setCallbackFunction = _ => CalculateFriction();

            softColliderFriction = new JSONStorableFloat("softColliderFriction", 1, 0, 1);

            _glossJsf = skinMaterials.GetFloatJSONParam("Gloss");
            _glossJsf.setJSONCallbackFunction = _ => CalculateFriction();

            _specularBumpinessJsf = skinMaterials.GetFloatJSONParam("Specular Bumpiness");
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();

            CalculateFriction();
        }

        public static void Refresh(JSONStorable skinMaterialsStorable)
        {
            skinMaterials = skinMaterialsStorable;

            _glossJsf = skinMaterials.GetFloatJSONParam("Gloss");
            _glossJsf.setJSONCallbackFunction = _ => CalculateFriction();

            _specularBumpinessJsf = skinMaterials.GetFloatJSONParam("Specular Bumpiness");
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();

            CalculateFriction();
        }

        /* Maximum friction that a collider can have, drops off dynamically with distance from collider's normal position */
        private static void CalculateFriction()
        {
            if(!enableAdaptiveFriction.val)
            {
                maxHardColliderFriction = 0.600f;
                softColliderFriction.val = 0.600f;
            }
            else
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
        }

        public static void RemoveCallbacks()
        {
            _glossJsf.setJSONCallbackFunction = null;
            _specularBumpinessJsf.setJSONCallbackFunction = null;
        }
    }
}
