using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using UnityEngine;

namespace TittyMagic
{
    internal class SoftPhysicsHandler
    {
        private readonly DAZCharacterSelector _geometry;
        private readonly DAZPhysicsMesh _breastPhysicsMesh;
        private readonly List<string> _breastPhysicsMeshFloatParamNames;
        private Dictionary<string, float> _originalBreastPhysicsMeshFloats;
        private bool _originalAutoFatColliderRadius;
        private bool _originalHardColliders;
        private bool _originalSelfCollision;
        private Dictionary<string, bool> _originalGroupsUseParentSettings;

        private List<StaticPhysicsConfig> _configs;
        private List<StaticPhysicsConfig> _nippleConfigs;

        private float _softVerticesCombinedSpringLeft;
        private float _softVerticesCombinedSpringRight;
        private float _softVerticesCombinedDamperLeft;
        private float _softVerticesCombinedDamperRight;
        private float _softVerticesMassLeft;
        private float _softVerticesMassRight;
        private float _softVerticesColliderRadiusLeft;
        private float _softVerticesColliderRadiusRight;
        private float _softVerticesNormalLimitLeft;
        private float _softVerticesNormalLimitRight;
        private float _softVerticesBackForceLeft;
        private float _softVerticesBackForceRight;
        private float _softVerticesBackForceThresholdDistanceLeft;
        private float _softVerticesBackForceThresholdDistanceRight;
        private float _softVerticesBackForceMaxForceLeft;
        private float _softVerticesBackForceMaxForceRight;
        private float _groupASpringMultiplierLeft;
        private float _groupASpringMultiplierRight;
        private float _groupADamperMultiplierLeft;
        private float _groupADamperMultiplierRight;
        private float _groupBSpringMultiplierLeft;
        private float _groupBSpringMultiplierRight;
        private float _groupBDamperMultiplierLeft;
        private float _groupBDamperMultiplierRight;
        private float _groupCSpringMultiplierRight;
        private float _groupCSpringMultiplierLeft;
        private float _groupCDamperMultiplierLeft;
        private float _groupCDamperMultiplierRight;
        private float _groupDSpringMultiplierRight;
        private float _groupDSpringMultiplierLeft;
        private float _groupDDamperMultiplierLeft;
        private float _groupDDamperMultiplierRight;

        public SoftPhysicsHandler(
            DAZPhysicsMesh breastPhysicsMesh,
            DAZCharacterSelector geometry
        )
        {
            _geometry = geometry;
            _breastPhysicsMesh = breastPhysicsMesh;
            _breastPhysicsMeshFloatParamNames = _breastPhysicsMesh.GetFloatParamNames();
            SaveOriginalPhysicsAndSetPluginDefaults();
        }

        public void LoadSettings()
        {
            LoadSoftPhysicsSettings();
            LoadNipplePhysicsSettings();
        }

        private void LoadSoftPhysicsSettings()
        {
            var softVerticesCombinedSpring = new StaticPhysicsConfig(500f, 500f, 62f);
            softVerticesCombinedSpring.SetLinearCurvesAroundMidpoint(slope: 0.41f);

            var softVerticesCombinedDamper = new StaticPhysicsConfig(10.0f, 10.0f, 0.90f)
            {
                dependOnPhysicsRate = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.75f, -0.90f, -0.45f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(1.125f, 1.35f, 0.675f),
            };
            softVerticesCombinedDamper.SetLinearCurvesAroundMidpoint(slope: 0.082f);

            var softVerticesMass = new StaticPhysicsConfig(0.050f, 0.130f, 0.085f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, -0.048f, -0.028f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.012f, 0.060f, 0.040f),
            };
            var softVerticesColliderRadius = new StaticPhysicsConfig(0.024f, 0.037f, 0.028f)
            {
                useRealMass = true,
            };
            var softVerticesNormalLimit = new StaticPhysicsConfig(0.020f, 0.068f, 0.028f)
            {
                useRealMass = true,
                quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, 0.024f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, -0.008f),
            };
            var softVerticesBackForce = new StaticPhysicsConfig(50f, 55.6f, 9.3f)
            {
                quicknessOffsetConfig = new StaticPhysicsConfigBase(-2.6f, -4f, -2.33f),
                slownessOffsetConfig = new StaticPhysicsConfigBase(0.8f, 1.33f, 0.77f),
            };
            softVerticesBackForce.SetLinearCurvesAroundMidpoint(slope: 0.027f);

            var softVerticesBackForceThresholdDistance = new StaticPhysicsConfig(0f, 0f, 0f);
            var softVerticesBackForceMaxForce = new StaticPhysicsConfig(50f, 50f, 50f);
            var groupASpringMultiplier = new StaticPhysicsConfig(5f, 5f, 1f);
            groupASpringMultiplier.SetLinearCurvesAroundMidpoint(slope: 0);
            var groupADamperMultiplier = new StaticPhysicsConfig(1f, 1f, 1f);
            var groupBSpringMultiplier = new StaticPhysicsConfig(5f, 5f, 1f);
            groupBSpringMultiplier.SetLinearCurvesAroundMidpoint(slope: 0);
            var groupBDamperMultiplier = new StaticPhysicsConfig(1f, 1f, 1f);
            var groupCSpringMultiplier = new StaticPhysicsConfig(2.29f, 1.30f, 2.29f);
            var groupCDamperMultiplier = new StaticPhysicsConfig(1.81f, 1.22f, 1.81f);

            softVerticesCombinedSpring.updateFunction = value =>
            {
                _softVerticesCombinedSpringLeft = value;
                _softVerticesCombinedSpringRight = value;
            };
            softVerticesCombinedDamper.updateFunction = value =>
            {
                _softVerticesCombinedDamperLeft = value;
                _softVerticesCombinedDamperRight = value;
            };
            softVerticesMass.updateFunction = value =>
            {
                _softVerticesMassLeft = value;
                _softVerticesMassRight = value;
                _breastPhysicsMesh.softVerticesGroups
                    .ForEach(group => group.jointMass = value);
            };
            softVerticesColliderRadius.updateFunction = value =>
            {
                _softVerticesColliderRadiusLeft = value;
                _softVerticesColliderRadiusRight = value;
                _breastPhysicsMesh.softVerticesGroups.ForEach(group =>
                {
                    if(group.useParentColliderSettings)
                    {
                        group.colliderRadiusNoSync = value;
                        group.colliderNormalOffsetNoSync = value;
                    }
                    if(group.useParentColliderSettingsForSecondCollider)
                    {
                        group.secondColliderRadiusNoSync = value;
                        group.secondColliderNormalOffsetNoSync = value;
                    }
                    if(group.colliderSyncDirty)
                        group.SyncColliders();
                });
            };
            softVerticesNormalLimit.updateFunction = value =>
            {
                _softVerticesNormalLimitLeft = value;
                _softVerticesNormalLimitRight = value;
                _breastPhysicsMesh.softVerticesGroups
                    .ForEach(group => group.normalDistanceLimit = value);
            };
            softVerticesBackForce.updateFunction = value =>
            {
                _softVerticesBackForceLeft = value;
                _softVerticesBackForceRight = value;
                _breastPhysicsMesh.softVerticesGroups
                    .ForEach(group => group.jointBackForce = value);
            };
            softVerticesBackForceThresholdDistance.updateFunction = value =>
            {
                _softVerticesBackForceThresholdDistanceLeft = value;
                _softVerticesBackForceThresholdDistanceRight = value;
                _breastPhysicsMesh.softVerticesGroups
                    .ForEach(group => group.jointBackForceThresholdDistance = value);
            };
            softVerticesBackForceMaxForce.updateFunction = value =>
            {
                _softVerticesBackForceMaxForceLeft = value;
                _softVerticesBackForceMaxForceRight = value;
                _breastPhysicsMesh.softVerticesGroups
                    .ForEach(group => group.jointBackForceMaxForce = value);
            };
            groupASpringMultiplier.updateFunction = value =>
            {
                _groupASpringMultiplierLeft = value;
                _groupASpringMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupASlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupADamperMultiplier.updateFunction = value =>
            {
                _groupADamperMultiplierLeft = value;
                _groupADamperMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupASlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };
            groupBSpringMultiplier.updateFunction = value =>
            {
                _groupBSpringMultiplierLeft = value;
                _groupBSpringMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupBSlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupBDamperMultiplier.updateFunction = value =>
            {
                _groupBDamperMultiplierLeft = value;
                _groupBDamperMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupBSlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };
            groupCSpringMultiplier.updateFunction = value =>
            {
                _groupCSpringMultiplierLeft = value;
                _groupCSpringMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupCSlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupCDamperMultiplier.updateFunction = value =>
            {
                _groupCDamperMultiplierLeft = value;
                _groupCDamperMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupCSlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };

            _configs = new List<StaticPhysicsConfig>
            {
                softVerticesCombinedSpring,
                softVerticesCombinedDamper,
                softVerticesMass,
                softVerticesColliderRadius,
                softVerticesNormalLimit,
                softVerticesBackForce,
                softVerticesBackForceThresholdDistance,
                softVerticesBackForceMaxForce,
                groupASpringMultiplier,
                groupADamperMultiplier,
                groupBSpringMultiplier,
                groupBDamperMultiplier,
                groupCSpringMultiplier,
                groupCDamperMultiplier,
            };
        }

        private void LoadNipplePhysicsSettings()
        {
            var groupDSpringMultiplier = new StaticPhysicsConfig(2.29f, 1.30f, 2.29f);
            var groupDDamperMultiplier = new StaticPhysicsConfig(1.81f, 1.22f, 1.81f);

            groupDSpringMultiplier.updateFunction = value =>
            {
                _groupDSpringMultiplierLeft = value;
                _groupDSpringMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupDSlots)
                {
                    SyncGroupSpringMultiplier(slot, value);
                }
            };
            groupDDamperMultiplier.updateFunction = value =>
            {
                _groupDDamperMultiplierLeft = value;
                _groupDDamperMultiplierRight = value;
                foreach(int slot in _breastPhysicsMesh.groupDSlots)
                {
                    SyncGroupDamperMultiplier(slot, value);
                }
            };

            _nippleConfigs = new List<StaticPhysicsConfig>
            {
                groupDSpringMultiplier,
                groupDDamperMultiplier,
            };
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]SpringMultiplier and SyncSoftVerticesCombinedSpring
        // Circumvents use of softVerticesCombinedSpring value as multiplier on the group specific value, using custom multiplier instead
        private void SyncGroupSpringMultiplier(int slot, float value)
        {
            // Hack. Slot is even for right breast, odd for left breast.
            float combinedSpring = slot % 2 == 0
                ? _softVerticesCombinedSpringRight
                : _softVerticesCombinedSpringLeft;

            var group = _breastPhysicsMesh.softVerticesGroups[slot];
            float combinedValue = combinedSpring * value;
            group.jointSpringNormal = combinedValue;
            group.jointSpringTangent = combinedValue;
            group.jointSpringTangent2 = combinedValue;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkSpring = combinedValue;
            }
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]DamperMultiplier and SyncSoftVerticesCombinedDamper
        // Circumvents use of softVerticesCombinedDamper value as multiplier on the group specific value, using custom multiplier instead
        private void SyncGroupDamperMultiplier(int slot, float value)
        {
            // Hack. Slot is even for right breast, odd for left breast.
            float combinedDamper = slot % 2 == 0
                ? _softVerticesCombinedDamperRight
                : _softVerticesCombinedDamperLeft;

            var group = _breastPhysicsMesh.softVerticesGroups[slot];
            float combinedValue = combinedDamper * value;
            group.jointDamperNormal = combinedValue;
            group.jointDamperTangent = combinedValue;
            group.jointDamperTangent2 = combinedValue;
            if(group.tieLinkJointSpringAndDamperToNormalSpringAndDamper)
            {
                group.linkDamper = combinedValue;
            }
        }

        public void UpdatePhysics(
            float massAmount,
            float realMassAmount,
            float softnessAmount,
            float quicknessAmount
        )
        {
            foreach(var config in _configs)
            {
                float mass = config.useRealMass ? realMassAmount : massAmount;
                config.updateFunction(NewValue(config, mass, softnessAmount, quicknessAmount));
            }
        }

        public void UpdateNipplePhysics(float softnessAmount, float nippleErectionVal)
        {
            foreach(var config in _nippleConfigs)
            {
                config.updateFunction(NewNippleValue(config, softnessAmount, 1.25f * nippleErectionVal));
            }
        }

        public void UpdateRateDependentPhysics(
            float massAmount,
            float realMassAmount,
            float softnessAmount,
            float quicknessAmount
        )
        {
            foreach(var config in _configs)
            {
                if(config.dependOnPhysicsRate)
                {
                    float mass = config.useRealMass ? realMassAmount : massAmount;
                    config.updateFunction(NewValue(config, mass, softnessAmount, quicknessAmount));
                }
            }
        }

        //TODO is duplicate
        // input mass, softness and quickness normalized to (0,1) range
        private float NewValue(StaticPhysicsConfig config, float mass, float softness, float quickness)
        {
            float result = config.Calculate(mass, softness, quickness);
            return config.dependOnPhysicsRate ? PhysicsRateMultiplier() * result : result;
        }

        private static float NewNippleValue(StaticPhysicsConfigBase config, float mass, float softness, float addend = 0)
        {
            return config.Calculate(mass, softness) + addend;
        }

        //TODO is duplicate
        // see UserPreferences.cs methods SetPhysics45, 60, 72 etc.
        private static float PhysicsRateMultiplier()
        {
            return 0.01666667f / Time.fixedDeltaTime;
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            // auto fat collider radius off (no effect)
            _originalAutoFatColliderRadius = _breastPhysicsMesh.softVerticesUseAutoColliderRadius;
            _breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;
            // hard colliders off
            _originalHardColliders = _geometry.useAuxBreastColliders;
            _geometry.useAuxBreastColliders = false;
            // self colliders off
            _originalSelfCollision = _breastPhysicsMesh.allowSelfCollision;
            _breastPhysicsMesh.allowSelfCollision = true;
            // TODO configurable
            _breastPhysicsMesh.softVerticesColliderAdditionalNormalOffset = 0.001f;
            // prevent settings in F Breast Physics 2 from having effect
            _originalGroupsUseParentSettings = new Dictionary<string, bool>();
            foreach(var group in _breastPhysicsMesh.softVerticesGroups)
            {
                _originalGroupsUseParentSettings[group.name] = group.useParentSettings;
                group.useParentSettings = false;
            }

            _originalBreastPhysicsMeshFloats = new Dictionary<string, float>();
            foreach(string name in _breastPhysicsMeshFloatParamNames)
            {
                var param = _breastPhysicsMesh.GetFloatJSONParam(name);
                _originalBreastPhysicsMeshFloats[name] = param.val;
                param.val = 0;
            }
        }

        public void RestoreOriginalPhysics()
        {
            _breastPhysicsMesh.softVerticesUseAutoColliderRadius = _originalAutoFatColliderRadius;
            _geometry.useAuxBreastColliders = _originalHardColliders;
            _breastPhysicsMesh.allowSelfCollision = _originalSelfCollision;
            foreach(var group in _breastPhysicsMesh.softVerticesGroups)
            {
                group.useParentSettings = _originalGroupsUseParentSettings[group.name];
            }
            foreach(string name in _breastPhysicsMeshFloatParamNames)
            {
                _breastPhysicsMesh.GetFloatJSONParam(name).val = _originalBreastPhysicsMeshFloats[name];
            }
        }

        public JSONClass Serialize()
        {
            var json = new JSONClass();
            json["breastPhysicsMeshFloats"] = JSONArrayFromDictionary(_originalBreastPhysicsMeshFloats);
            json["autoFatColliderRadius"].AsBool = _originalAutoFatColliderRadius;
            json["hardColliders"].AsBool = _originalHardColliders;
            json["selfCollision"].AsBool = _originalSelfCollision;
            json["groupsUseParentSettings"] = JSONArrayFromDictionary(_originalGroupsUseParentSettings);
            return json;
        }

        //TODO is duplicate
        private static JSONArray JSONArrayFromDictionary(Dictionary<string, float> dictionary)
        {
            var jsonArray = new JSONArray();
            foreach(var kvp in dictionary)
            {
                var entry = new JSONClass();
                entry["paramName"] = kvp.Key;
                entry["value"].AsFloat = kvp.Value;
                jsonArray.Add(entry);
            }
            return jsonArray;
        }

        private static JSONArray JSONArrayFromDictionary(Dictionary<string, bool> dictionary)
        {
            var jsonArray = new JSONArray();
            foreach(var kvp in dictionary)
            {
                var entry = new JSONClass();
                entry["paramName"] = kvp.Key;
                entry["value"].AsBool = kvp.Value;
                jsonArray.Add(entry);
            }
            return jsonArray;
        }

        public void RestoreFromJSON(JSONClass originalPhysicsJSON)
        {

            var breastPhysicsMeshFloats = originalPhysicsJSON["breastPhysicsMeshFloats"].AsArray;
            foreach(JSONClass json in breastPhysicsMeshFloats)
            {
                _originalBreastPhysicsMeshFloats[json["paramName"].Value] = json["value"].AsFloat;
            }

            _originalAutoFatColliderRadius = originalPhysicsJSON["autoFatColliderRadius"].AsBool;
            _originalHardColliders = originalPhysicsJSON["hardColliders"].AsBool;
            _originalSelfCollision = originalPhysicsJSON["selfCollision"].AsBool;

            var groupsUseParentSettings = originalPhysicsJSON["groupsUseParentSettings"].AsArray;
            foreach(JSONClass json in groupsUseParentSettings)
            {
                _originalGroupsUseParentSettings[json["paramName"].Value] = json["value"].AsBool;
            }
        }
    }
}
