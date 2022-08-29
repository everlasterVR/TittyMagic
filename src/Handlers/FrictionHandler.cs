using TittyMagic.Configs;
using TittyMagic.Handlers;
using TittyMagic.UI;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public static class FrictionHandler
    {
        public static JSONStorable skinMaterials { get; private set; }
        private static PhysicsParameterGroup _colliderRadiusParameter;

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
            softColliderFriction.setCallbackFunction = UpdateSoftColliders;

            _glossJsf = skinMaterials.GetFloatJSONParam("Gloss");
            _glossJsf.setJSONCallbackFunction = _ => CalculateFriction();

            _specularBumpinessJsf = skinMaterials.GetFloatJSONParam("Specular Bumpiness");
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();
        }

        private static DynamicPhysicsConfig NewSoftVerticesColliderRadiusConfig() =>
            new DynamicPhysicsConfig(
                massMultiplier: 0.0060f,
                softnessMultiplier: 0,
                isNegative: false,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: x => 1.75f * Curves.InverseSmoothStep(x, 1, 0.4f, 0.7f)
            );

        public static void LoadSettings()
        {
            _colliderRadiusParameter = SoftPhysicsHandler.parameterGroups[ParamName.SOFT_VERTICES_COLLIDER_RADIUS];
            _colliderRadiusParameter.SetFrictionConfig(
                NewSoftVerticesColliderRadiusConfig(),
                NewSoftVerticesColliderRadiusConfig()
            );
        }

        private static void UpdateSoftColliders(float friction)
        {
            if(enableAdaptiveFriction.val)
            {
                /* Update physics dependent on friction */
                float mass = MainPhysicsHandler.realMassAmount;
                float softness = tittyMagic.softnessAmount;
                float inverseFriction = 1 - Mathf.InverseLerp(0, drySkinFriction.val, friction);
                _colliderRadiusParameter.UpdateInverseFrictionValue(inverseFriction, mass, softness);
            }
            else
            {
                _colliderRadiusParameter.ResetInverseFrictionValue();
            }

            /* Update collider friction */
            SoftPhysicsHandler.SyncFriction(friction);
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
        public static void CalculateFriction()
        {
            if(!enableAdaptiveFriction.val)
            {
                maxHardColliderFriction = 0.600f;
                softColliderFriction.valNoCallback = 0.600f;
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
                    softColliderFriction.valNoCallback = Mathf.Lerp(minFriction, 1.000f, specBumpComponent);
                }
            }

            UpdateSoftColliders(softColliderFriction.val);
        }

        public static void RemoveCallbacks()
        {
            _glossJsf.setJSONCallbackFunction = null;
            _specularBumpinessJsf.setJSONCallbackFunction = null;
        }
    }
}
