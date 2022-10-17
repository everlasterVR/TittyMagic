using TittyMagic.Handlers.Configs;
using TittyMagic.Handlers;
using TittyMagic.Models;
using UnityEngine;
using static TittyMagic.Script;

namespace TittyMagic
{
    public static class FrictionHandler
    {
        private static JSONStorable _skinMaterialsStorable;
        private static PhysicsParameterGroup _colliderRadiusParameter;

        public static JSONStorableFloat frictionOffsetJsf { get; private set; }
        public static JSONStorableFloat softColliderFrictionJsf { get; private set; }
        public static JSONStorableBool adaptiveFrictionJsb { get; private set; }
        public static JSONStorableFloat drySkinFrictionJsf { get; private set; }
        public static float maxHardColliderFriction { get; private set; }

        private static JSONStorableFloat _glossJsf;
        private static JSONStorableFloat _specularBumpinessJsf;

        public static bool enabled { get; private set; }

        public static void Init()
        {
            if(!personIsFemale)
            {
                return;
            }

            frictionOffsetJsf = tittyMagic.NewJSONStorableFloat("frictionOffset", 0, -0.5f, 0.5f);
            frictionOffsetJsf.setCallbackFunction = _ => CalculateFriction();

            softColliderFrictionJsf = new JSONStorableFloat("softColliderFriction", HardCollider.DEFAULT_FRICTION, 0, 1);
            softColliderFrictionJsf.setCallbackFunction = UpdateSoftColliders;

            adaptiveFrictionJsb = tittyMagic.NewJSONStorableBool("enableAdaptiveFriction", false);
            adaptiveFrictionJsb.setCallbackFunction = val =>
            {
                if(tittyMagic.drySkinFrictionSlider != null)
                {
                    tittyMagic.drySkinFrictionSlider.SetActiveStyle(val);
                }

                CalculateFriction();
            };

            drySkinFrictionJsf = tittyMagic.NewJSONStorableFloat("drySkinFriction", 0.750f, 0.000f, 1.000f);
            drySkinFrictionJsf.setCallbackFunction = _ => CalculateFriction();

            _skinMaterialsStorable = tittyMagic.containingAtom.GetStorableByID("skin");
            if(_skinMaterialsStorable == null)
            {
                enabled = false;
            }
            else
            {
                ResetGlossAndSpecularStorables();
                enabled = true;
            }
        }

        public static void Refresh()
        {
            _skinMaterialsStorable = tittyMagic.containingAtom.GetStorableByID("skin");
            if(!personIsFemale || _skinMaterialsStorable == null)
            {
                enabled = false;
            }
            else
            {
                ResetGlossAndSpecularStorables();
                LoadSettings();
                CalculateFriction();
                enabled = true;
            }
        }

        private static DynamicPhysicsConfig NewSoftVerticesColliderRadiusConfig() =>
            new DynamicPhysicsConfig(
                massMultiplier: 0.0060f,
                softnessMultiplier: 0,
                negative: false,
                applyMethod: ApplyMethod.ADDITIVE,
                massCurve: x => 1.75f * Curves.InverseSmoothStep(x, 1, 0.4f, 0.7f)
            );

        public static void LoadSettings()
        {
            if(!enabled)
            {
                return;
            }

            _colliderRadiusParameter = SoftPhysicsHandler.parameterGroups[ParamName.SOFT_VERTICES_COLLIDER_RADIUS];
            _colliderRadiusParameter.SetFrictionConfig(
                NewSoftVerticesColliderRadiusConfig(),
                NewSoftVerticesColliderRadiusConfig()
            );
        }

        private static void UpdateSoftColliders(float friction)
        {
            if(adaptiveFrictionJsb.val)
            {
                /* Update physics dependent on friction */
                float mass = MainPhysicsHandler.realMassAmount;
                float softness = tittyMagic.softnessAmount;
                float inverseFriction = 1 - Mathf.InverseLerp(0, drySkinFrictionJsf.val, friction);
                _colliderRadiusParameter.UpdateInverseFrictionValue(inverseFriction, mass, softness);
            }
            else
            {
                _colliderRadiusParameter.ResetInverseFrictionValue();
            }

            /* Update collider friction */
            SoftPhysicsHandler.SyncFriction(friction);
        }

        private static void ResetGlossAndSpecularStorables()
        {
            _glossJsf = _skinMaterialsStorable.GetFloatJSONParam("Gloss");
            _glossJsf.setJSONCallbackFunction = _ => CalculateFriction();

            _specularBumpinessJsf = _skinMaterialsStorable.GetFloatJSONParam("Specular Bumpiness");
            _specularBumpinessJsf.setJSONCallbackFunction = _ => CalculateFriction();
        }

        /* Maximum friction that a collider can have, drops off dynamically with distance from collider's normal position */
        public static void CalculateFriction()
        {
            if(!enabled || !tittyMagic.enabled)
            {
                return;
            }

            if(!adaptiveFrictionJsb.val)
            {
                float friction = Mathf.Clamp(HardCollider.DEFAULT_FRICTION + frictionOffsetJsf.val, 0, 1);
                maxHardColliderFriction = friction;
                softColliderFrictionJsf.valNoCallback = friction;
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
                float minFriction = drySkinFrictionJsf.val * (1 - Curves.InverseSmoothStep(normalizedLinearGloss, 1, 0.56f, 0.86f));

                /* Hard colliders */
                {
                    float specBumpComponent = 0.50f * bumpinessEffectMultiplier * normalizedSpecBump;
                    float friction = 0.80f * Mathf.Lerp(minFriction, 1.000f, specBumpComponent);
                    maxHardColliderFriction = Mathf.Clamp(friction + frictionOffsetJsf.val, 0, 1);
                }

                /* Soft colliders */
                {
                    float specBumpComponent = 0.60f * bumpinessEffectMultiplier * normalizedSpecBump;
                    float friction = Mathf.Lerp(minFriction, 1.000f, specBumpComponent);
                    softColliderFrictionJsf.valNoCallback = Mathf.Clamp(friction + frictionOffsetJsf.val, 0, 1);
                }
            }

            UpdateSoftColliders(softColliderFrictionJsf.val);
        }

        public static void Destroy()
        {
            if(_glossJsf != null)
            {
                _glossJsf.setJSONCallbackFunction = null;
            }

            if(_specularBumpinessJsf != null)
            {
                _specularBumpinessJsf.setJSONCallbackFunction = null;
            }

            /* ensure GC */
            _skinMaterialsStorable = null;
            _colliderRadiusParameter = null;
            frictionOffsetJsf = null;
            softColliderFrictionJsf = null;
            adaptiveFrictionJsb = null;
            drySkinFrictionJsf = null;
            _glossJsf = null;
            _specularBumpinessJsf = null;
        }
    }
}
