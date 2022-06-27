using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using TittyMagic.Configs;
using static TittyMagic.MVRParamName;

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

        private readonly DAZPhysicsMeshSoftVerticesGroup _mainLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _mainRight;
        private readonly DAZPhysicsMeshSoftVerticesGroup _outerLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _outerRight;
        private readonly DAZPhysicsMeshSoftVerticesGroup _areolaLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _areolaRight;
        private readonly DAZPhysicsMeshSoftVerticesGroup _nippleLeft;
        private readonly DAZPhysicsMeshSoftVerticesGroup _nippleRight;

        public Dictionary<string, PhysicsParameter> leftBreastParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> rightBreastParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> leftNippleParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> rightNippleParameters { get; private set; }

        private float _combinedSpringLeft;
        private float _combinedSpringRight;
        private float _combinedDamperLeft;
        private float _combinedDamperRight;

        public SoftPhysicsHandler(
            DAZPhysicsMesh breastPhysicsMesh,
            DAZCharacterSelector geometry
        )
        {
            _geometry = geometry;
            _breastPhysicsMesh = breastPhysicsMesh;
            _mainLeft = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "left");
            _mainRight = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "right");
            _outerLeft = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "leftouter");
            _outerRight = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "rightouter");
            _areolaLeft = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "leftareola");
            _areolaRight = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "rightareola");
            _nippleLeft = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "leftnipple");
            _nippleRight = breastPhysicsMesh.softVerticesGroups.Find(group => group.name == "rightnipple");

            _breastPhysicsMeshFloatParamNames = _breastPhysicsMesh.GetFloatParamNames();
            SaveOriginalPhysicsAndSetPluginDefaults();
        }

        public void LoadSettings()
        {
            SetupPhysicsParameters(true);
            SetupPhysicsParameters(false);
        }

        private void SetupPhysicsParameters(bool leftBreast)
        {
            var softVerticesCombinedSpring = new PhysicsParameter("Fat Spring");
            var softVerticesCombinedDamper = new PhysicsParameter("Fat Damper");
            var softVerticesMass = new PhysicsParameter("Fat Mass");
            var softVerticesColliderRadius = new PhysicsParameter("Fat Collider Radius");
            var softVerticesDistanceLimit = new PhysicsParameter("Fat Distance Limit");
            var softVerticesBackForce = new PhysicsParameter("Fat Back Force");
            var softVerticesBackForceThresholdDistance = new PhysicsParameter("Fat Bk Force Threshold");
            var softVerticesBackForceMaxForce = new PhysicsParameter("Fat Bk Force Max Force");
            var groupASpringMultiplier = new PhysicsParameter("Main Spring Multiplier");
            var groupADamperMultiplier = new PhysicsParameter("Main Damper Multiplier");
            var groupBSpringMultiplier = new PhysicsParameter("Outer Spring Multiplier");
            var groupBDamperMultiplier = new PhysicsParameter("Outer Damper Multiplier");
            var groupCSpringMultiplier = new PhysicsParameter("Areola Spring Multiplier");
            var groupCDamperMultiplier = new PhysicsParameter("Areola Damper Multiplier");
            var groupDSpringMultiplier = new PhysicsParameter("Nipple Spring Multiplier");
            var groupDDamperMultiplier = new PhysicsParameter("Nipple Damper Multiplier");

            softVerticesCombinedSpring.config = new StaticPhysicsConfig(500f, 500f, 62f);
            softVerticesCombinedSpring.config.SetLinearCurvesAroundMidpoint(slope: 0.41f);

            softVerticesCombinedDamper.config = new StaticPhysicsConfig(10.0f, 10.0f, 0.90f);
            softVerticesCombinedDamper.config.dependOnPhysicsRate = true;
            softVerticesCombinedDamper.config.SetLinearCurvesAroundMidpoint(slope: 0.082f);
            softVerticesCombinedDamper.quicknessOffsetConfig = new StaticPhysicsConfigBase(-0.75f, -0.90f, -0.45f);
            softVerticesCombinedDamper.slownessOffsetConfig = new StaticPhysicsConfigBase(1.125f, 1.35f, 0.675f);

            softVerticesMass.config = new StaticPhysicsConfig(0.050f, 0.130f, 0.085f);
            softVerticesMass.quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, -0.048f, -0.028f);
            softVerticesMass.slownessOffsetConfig = new StaticPhysicsConfigBase(0.012f, 0.060f, 0.040f);

            softVerticesColliderRadius.config = new StaticPhysicsConfig(0.024f, 0.037f, 0.028f);
            softVerticesColliderRadius.config.useRealMass = true;

            softVerticesDistanceLimit.config = new StaticPhysicsConfig(0.020f, 0.068f, 0.028f);
            softVerticesDistanceLimit.config.useRealMass = true;
            softVerticesDistanceLimit.quicknessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, 0.024f);
            softVerticesDistanceLimit.slownessOffsetConfig = new StaticPhysicsConfigBase(0.000f, 0.000f, -0.008f);

            softVerticesBackForce.config = new StaticPhysicsConfig(50f, 55.6f, 9.3f);
            softVerticesBackForce.config.SetLinearCurvesAroundMidpoint(slope: 0.027f);
            softVerticesBackForce.quicknessOffsetConfig = new StaticPhysicsConfigBase(-2.6f, -4f, -2.33f);
            softVerticesBackForce.slownessOffsetConfig = new StaticPhysicsConfigBase(0.8f, 1.33f, 0.77f);

            softVerticesBackForceThresholdDistance.config = new StaticPhysicsConfig(0f, 0f, 0f);
            softVerticesBackForceMaxForce.config = new StaticPhysicsConfig(50f, 50f, 50f);

            groupASpringMultiplier.config = new StaticPhysicsConfig(5f, 5f, 1f);
            groupASpringMultiplier.config.SetLinearCurvesAroundMidpoint(slope: 0);
            groupADamperMultiplier.config = new StaticPhysicsConfig(1f, 1f, 1f);

            groupBSpringMultiplier.config = new StaticPhysicsConfig(5f, 5f, 1f);
            groupBSpringMultiplier.config.SetLinearCurvesAroundMidpoint(slope: 0);
            groupBDamperMultiplier.config = new StaticPhysicsConfig(1f, 1f, 1f);

            groupCSpringMultiplier.config = new StaticPhysicsConfig(2.29f, 1.30f, 2.29f);
            groupCDamperMultiplier.config = new StaticPhysicsConfig(1.81f, 1.22f, 1.81f);

            groupDSpringMultiplier.config = new StaticPhysicsConfig(2.29f, 1.30f, 2.29f);
            groupDDamperMultiplier.config = new StaticPhysicsConfig(1.81f, 1.22f, 1.81f);

            if(leftBreast)
            {
                softVerticesCombinedSpring.sync = value => { _combinedSpringLeft = value; };
                softVerticesCombinedDamper.sync = value => { _combinedDamperLeft = value; };
                softVerticesMass.sync = SyncMassLeft;
                softVerticesBackForce.sync = SyncBackForceLeft;
                softVerticesBackForceThresholdDistance.sync = SyncBackForceThresholdDistanceLeft;
                softVerticesBackForceMaxForce.sync = SyncBackForceMaxForceLeft;
                softVerticesColliderRadius.sync = SyncColliderRadiusLeft;
                // softVerticesColliderAdditionalNormalOffset.sync = SyncAdditionalNormalOffsetLeft;
                softVerticesDistanceLimit.sync = SyncDistanceLimitLeft;
                groupASpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringLeft, _mainLeft);
                groupADamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperLeft, _mainLeft);
                groupBSpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringLeft, _outerLeft);
                groupBDamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperLeft, _outerLeft);
                groupCSpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringLeft, _areolaLeft);
                groupCDamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperLeft, _areolaLeft);
                groupDSpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringLeft, _nippleLeft);
                groupDDamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperLeft, _nippleLeft);
            }
            else
            {
                softVerticesCombinedSpring.sync = value => { _combinedSpringRight = value; };
                softVerticesCombinedDamper.sync = value => { _combinedDamperRight = value; };
                softVerticesMass.sync = SyncMassRight;
                softVerticesBackForce.sync = SyncBackForceRight;
                softVerticesBackForceThresholdDistance.sync = SyncBackForceThresholdDistanceRight;
                softVerticesBackForceMaxForce.sync = SyncBackForceMaxForceRight;
                softVerticesColliderRadius.sync = SyncColliderRadiusRight;
                // softVerticesColliderAdditionalNormalOffset.sync = SyncAdditionalNormalOffsetRight;
                softVerticesDistanceLimit.sync = SyncDistanceLimitRight;
                groupASpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringRight, _mainRight);
                groupADamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperRight, _mainRight);
                groupBSpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringRight, _outerRight);
                groupBDamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperRight, _outerRight);
                groupCSpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringRight, _areolaRight);
                groupCDamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperRight, _areolaRight);
                groupDSpringMultiplier.sync = value => SyncGroupSpringMultiplier(value, _combinedSpringRight, _nippleRight);
                groupDDamperMultiplier.sync = value => SyncGroupDamperMultiplier(value, _combinedDamperRight, _nippleRight);
            }

            var breastParameters = new Dictionary<string, PhysicsParameter>
            {
                { SOFT_VERTICES_COMBINED_SPRING, softVerticesCombinedSpring },
                { SOFT_VERTICES_COMBINED_DAMPER, softVerticesCombinedDamper },
                { SOFT_VERTICES_MASS, softVerticesMass },
                { SOFT_VERTICES_BACK_FORCE, softVerticesBackForce },
                { SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, softVerticesBackForceThresholdDistance },
                { SOFT_VERTICES_BACK_FORCE_MAX_FORCE, softVerticesBackForceMaxForce },
                { SOFT_VERTICES_COLLIDER_RADIUS, softVerticesColliderRadius },
                // { SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, softVerticesColliderAdditionalNormalOffset },
                { SOFT_VERTICES_DISTANCE_LIMIT, softVerticesDistanceLimit },
                { GROUP_A_SPRING_MULTIPLIER, groupASpringMultiplier },
                { GROUP_A_DAMPER_MULTIPLIER, groupADamperMultiplier },
                { GROUP_B_SPRING_MULTIPLIER, groupBSpringMultiplier },
                { GROUP_B_DAMPER_MULTIPLIER, groupBDamperMultiplier },
                { GROUP_C_SPRING_MULTIPLIER, groupCSpringMultiplier },
                { GROUP_C_DAMPER_MULTIPLIER, groupCDamperMultiplier },
            };
            var nippleParameters = new Dictionary<string, PhysicsParameter>
            {
                { GROUP_D_SPRING_MULTIPLIER, groupDSpringMultiplier },
                { GROUP_D_DAMPER_MULTIPLIER, groupDDamperMultiplier },
            };

            if(leftBreast)
            {
                leftBreastParameters = breastParameters;
                leftNippleParameters = nippleParameters;
            }
            else
            {
                rightBreastParameters = breastParameters;
                rightNippleParameters = nippleParameters;
            }
        }

        private void SyncMassLeft(float value)
        {
            _mainLeft.jointMass = value;
            _outerLeft.jointMass = value;
            _areolaLeft.jointMass = value;
            _nippleLeft.jointMass = value;
        }

        private void SyncMassRight(float value)
        {
            _mainRight.jointMass = value;
            _outerRight.jointMass = value;
            _areolaRight.jointMass = value;
            _nippleRight.jointMass = value;
        }

        private void SyncBackForceLeft(float value)
        {
            _mainLeft.jointBackForce = value;
            _outerLeft.jointBackForce = value;
            _areolaLeft.jointBackForce = value;
            _nippleLeft.jointBackForce = value;
        }

        private void SyncBackForceRight(float value)
        {
            _mainRight.jointBackForce = value;
            _outerRight.jointBackForce = value;
            _areolaRight.jointBackForce = value;
            _nippleRight.jointBackForce = value;
        }

        private void SyncBackForceThresholdDistanceLeft(float value)
        {
            _mainLeft.jointBackForceThresholdDistance = value;
            _outerLeft.jointBackForceThresholdDistance = value;
            _areolaLeft.jointBackForceThresholdDistance = value;
            _nippleLeft.jointBackForceThresholdDistance = value;
        }

        private void SyncBackForceThresholdDistanceRight(float value)
        {
            _mainRight.jointBackForceThresholdDistance = value;
            _outerRight.jointBackForceThresholdDistance = value;
            _areolaRight.jointBackForceThresholdDistance = value;
            _nippleRight.jointBackForceThresholdDistance = value;
        }

        private void SyncBackForceMaxForceLeft(float value)
        {
            _mainLeft.jointBackForceMaxForce = value;
            _outerLeft.jointBackForceMaxForce = value;
            _areolaLeft.jointBackForceMaxForce = value;
            _nippleLeft.jointBackForceMaxForce = value;
        }

        private void SyncBackForceMaxForceRight(float value)
        {
            _mainRight.jointBackForceMaxForce = value;
            _outerRight.jointBackForceMaxForce = value;
            _areolaRight.jointBackForceMaxForce = value;
            _nippleRight.jointBackForceMaxForce = value;
        }

        private void SyncDistanceLimitLeft(float value)
        {
            _mainLeft.normalDistanceLimit = value;
            _outerLeft.normalDistanceLimit = value;
            _areolaLeft.normalDistanceLimit = value;
            _nippleLeft.normalDistanceLimit = value;
        }

        private void SyncDistanceLimitRight(float value)
        {
            _mainRight.normalDistanceLimit = value;
            _outerRight.normalDistanceLimit = value;
            _areolaRight.normalDistanceLimit = value;
            _nippleRight.normalDistanceLimit = value;
        }

        private void SyncColliderRadiusLeft(float value)
        {
            SyncColliderRadius(value, _mainLeft);
            SyncColliderRadius(value, _outerLeft);
            SyncColliderRadius(value, _areolaLeft);
            SyncColliderRadius(value, _nippleLeft);
        }

        private void SyncColliderRadiusRight(float value)
        {
            SyncColliderRadius(value, _mainRight);
            SyncColliderRadius(value, _outerRight);
            SyncColliderRadius(value, _areolaRight);
            SyncColliderRadius(value, _nippleRight);
        }

        private static void SyncColliderRadius(float value, DAZPhysicsMeshSoftVerticesGroup group)
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
        }

        // Reimplements DAZPhysicsMesh.cs methods SyncGroup[A|B|C|D]SpringMultiplier and SyncSoftVerticesCombinedSpring
        // Circumvents use of softVerticesCombinedSpring value as multiplier on the group specific value, using custom multiplier instead
        private void SyncGroupSpringMultiplier(float value, float combinedSpring, DAZPhysicsMeshSoftVerticesGroup group)
        {
            // var group = _breastPhysicsMesh.softVerticesGroups[slot];
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
        private void SyncGroupDamperMultiplier(float value, float combinedDamper, DAZPhysicsMeshSoftVerticesGroup group)
        {
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
            leftBreastParameters.Values
                .Concat(rightBreastParameters.Values)
                .Where(param => param.config != null).ToList()
                .ForEach(param => UpdateParam(param, massAmount, realMassAmount, softnessAmount, quicknessAmount));
        }

        public void UpdateRateDependentPhysics(
            float massAmount,
            float realMassAmount,
            float softnessAmount,
            float quicknessAmount
        )
        {
            leftBreastParameters.Values.ToList()
                .Concat(rightBreastParameters.Values)
                .Where(param => param.config != null && param.config.dependOnPhysicsRate).ToList()
                .ForEach(param => UpdateParam(param, massAmount, realMassAmount, softnessAmount, quicknessAmount));
        }

        public void UpdateNipplePhysics(float massAmount, float softnessAmount, float nippleErection)
        {
            leftNippleParameters.Values
                .Concat(rightNippleParameters.Values)
                .Where(param => param.config != null).ToList()
                .ForEach(param => UpdateNippleParam(param, massAmount, softnessAmount, nippleErection));
        }

        private static void UpdateParam(PhysicsParameter param, float realMassAmount, float massAmount, float softnessAmount, float quicknessAmount)
        {
            float massValue = param.config.useRealMass ? realMassAmount : massAmount;
            float value = MainPhysicsHandler.NewBaseValue(param, massValue, softnessAmount, quicknessAmount);
            param.SetValue(value);
        }

        private static void UpdateNippleParam(PhysicsParameter param, float mass, float softness, float nippleErection)
        {
            param.SetValue(param.config.Calculate(mass, softness) + 1.25f * nippleErection);
        }

        public void SaveOriginalPhysicsAndSetPluginDefaults()
        {
            // auto fat collider radius off (no effect)
            _originalAutoFatColliderRadius = _breastPhysicsMesh.softVerticesUseAutoColliderRadius;
            _breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;

            // hard colliders off
            _originalHardColliders = _geometry.useAuxBreastColliders;
            _geometry.useAuxBreastColliders = false;

            // TODO soft physics on

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
