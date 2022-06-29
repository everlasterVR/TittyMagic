using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using TittyMagic.Configs;
using static TittyMagic.MVRParamName;
using static TittyMagic.Utils;

namespace TittyMagic
{
    internal class SoftPhysicsHandler
    {
        private readonly DAZPhysicsMesh _breastPhysicsMesh;
        private readonly List<string> _breastPhysicsMeshFloatParamNames;
        private Dictionary<string, float> _originalBreastPhysicsMeshFloats;
        private bool _originalSoftPhysicsOn;
        private bool _originalAllowSelfCollision;
        private bool _originalAutoFatColliderRadius;
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
        public Dictionary<string, PhysicsParameter> leftBreastGroupParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> rightBreastGroupParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> leftNippleGroupParameters { get; private set; }
        public Dictionary<string, PhysicsParameter> rightNippleGroupParameters { get; private set; }

        public JSONStorableBool softPhysicsOn { get; }
        public JSONStorableBool allowSelfCollision { get; }

        private float _combinedSpringLeft;
        private float _combinedSpringRight;
        private float _combinedDamperLeft;
        private float _combinedDamperRight;

        public SoftPhysicsHandler(MVRScript script)
        {
            _breastPhysicsMesh = (DAZPhysicsMesh) script.containingAtom.GetStorableByID("BreastPhysicsMesh");

            var groups = _breastPhysicsMesh.softVerticesGroups;
            _mainLeft = groups.Find(group => group.name == "left");
            _mainRight = groups.Find(group => group.name == "right");
            _outerLeft = groups.Find(group => group.name == "leftouter");
            _outerRight = groups.Find(group => group.name == "rightouter");
            _areolaLeft = groups.Find(group => group.name == "leftareola");
            _areolaRight = groups.Find(group => group.name == "rightareola");
            _nippleLeft = groups.Find(group => group.name == "leftnipple");
            _nippleRight = groups.Find(group => group.name == "rightnipple");

            softPhysicsOn = script.NewJSONStorableBool(SOFT_PHYSICS_ON, true);
            softPhysicsOn.setCallbackFunction = SyncSoftPhysicsOn;

            allowSelfCollision = script.NewJSONStorableBool(ALLOW_SELF_COLLISION, true);
            allowSelfCollision.setCallbackFunction = SyncAllowSelfCollision;

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
            var softVerticesCombinedSpring = new PhysicsParameter(
                "Fat Spring",
                NewBaseValueStorable(0, 500),
                null,
                "F2"
            );
            var softVerticesCombinedDamper = new PhysicsParameter(
                "Fat Damper",
                NewBaseValueStorable(0, 10),
                null,
                "F3"
            );
            var softVerticesMass = new PhysicsParameter(
                "Fat Mass",
                NewBaseValueStorable(0.05f, 0.5f),
                null,
                "F3"
            );
            var softVerticesColliderRadius = new PhysicsParameter(
                "Fat Collider Radius",
                NewBaseValueStorable(0, 0.07f),
                null,
                "F3"
            );
            var softVerticesColliderAdditionalNormalOffset = new PhysicsParameter(
                "Fat Collider Depth",
                NewBaseValueStorable(-0.01f, 0.01f),
                null,
                "F3"
            );
            var softVerticesDistanceLimit = new PhysicsParameter(
                "Fat Distance Limit",
                NewBaseValueStorable(0, 0.1f),
                null,
                "F3"
            );
            var softVerticesBackForce = new PhysicsParameter(
                "Fat Back Force",
                NewBaseValueStorable(0, 50),
                null,
                "F2"
            );
            var softVerticesBackForceMaxForce = new PhysicsParameter(
                "Fat Bk Force Max Force",
                NewBaseValueStorable(0, 50),
                null,
                "F2"
            );
            var softVerticesBackForceThresholdDistance = new PhysicsParameter(
                "Fat Bk Force Threshold",
                NewBaseValueStorable(0, 0.030f),
                null,
                "F3"
            );
            var groupASpringMultiplier = new PhysicsParameter(
                "Main Spring Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupADamperMultiplier = new PhysicsParameter(
                "Main Damper Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupBSpringMultiplier = new PhysicsParameter(
                "Outer Spring Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupBDamperMultiplier = new PhysicsParameter(
                "Outer Damper Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupCSpringMultiplier = new PhysicsParameter(
                "Areola Spring Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupCDamperMultiplier = new PhysicsParameter(
                "Areola Damper Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupDSpringMultiplier = new PhysicsParameter(
                "Nipple Spring Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );
            var groupDDamperMultiplier = new PhysicsParameter(
                "Nipple Damper Multiplier",
                NewBaseValueStorable(0, 5),
                null,
                "F3"
            );

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

            softVerticesColliderAdditionalNormalOffset.config = new StaticPhysicsConfig(0.001f, 0.001f, 0.001f);

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
                softVerticesBackForceMaxForce.sync = SyncBackForceMaxForceLeft;
                softVerticesBackForceThresholdDistance.sync = SyncBackForceThresholdDistanceLeft;
                softVerticesColliderRadius.sync = SyncColliderRadiusLeft;
                softVerticesColliderAdditionalNormalOffset.sync = SyncAdditionalNormalOffsetLeft;
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
                softVerticesBackForceMaxForce.sync = SyncBackForceMaxForceRight;
                softVerticesBackForceThresholdDistance.sync = SyncBackForceThresholdDistanceRight;
                softVerticesColliderRadius.sync = SyncColliderRadiusRight;
                softVerticesColliderAdditionalNormalOffset.sync = SyncAdditionalNormalOffsetRight;
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
                { SOFT_VERTICES_BACK_FORCE_MAX_FORCE, softVerticesBackForceMaxForce },
                { SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE, softVerticesBackForceThresholdDistance },
                { SOFT_VERTICES_COLLIDER_RADIUS, softVerticesColliderRadius },
                { SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET, softVerticesColliderAdditionalNormalOffset },
                { SOFT_VERTICES_DISTANCE_LIMIT, softVerticesDistanceLimit },
            };
            var groupParameters = new Dictionary<string, PhysicsParameter>
            {
                { GROUP_A_SPRING_MULTIPLIER, groupASpringMultiplier },
                { GROUP_A_DAMPER_MULTIPLIER, groupADamperMultiplier },
                { GROUP_B_SPRING_MULTIPLIER, groupBSpringMultiplier },
                { GROUP_B_DAMPER_MULTIPLIER, groupBDamperMultiplier },
                { GROUP_C_SPRING_MULTIPLIER, groupCSpringMultiplier },
                { GROUP_C_DAMPER_MULTIPLIER, groupCDamperMultiplier },
            };
            var nippleGroupParameters = new Dictionary<string, PhysicsParameter>
            {
                { GROUP_D_SPRING_MULTIPLIER, groupDSpringMultiplier },
                { GROUP_D_DAMPER_MULTIPLIER, groupDDamperMultiplier },
            };

            var texts = CreateInfoTexts();
            foreach(var param in breastParameters)
            {
                param.Value.infoText = texts[param.Key];
            }

            if(leftBreast)
            {
                leftBreastParameters = breastParameters;
                leftBreastGroupParameters = groupParameters;
                leftNippleGroupParameters = nippleGroupParameters;
            }
            else
            {
                rightBreastParameters = breastParameters;
                rightBreastGroupParameters = groupParameters;
                rightNippleGroupParameters = nippleGroupParameters;
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

        private void SyncAdditionalNormalOffsetLeft(float value)
        {
            _mainLeft.colliderAdditionalNormalOffset = value;
            _outerLeft.colliderAdditionalNormalOffset = value;
            _areolaLeft.colliderAdditionalNormalOffset = value;
            _nippleLeft.colliderAdditionalNormalOffset = value;
        }

        private void SyncAdditionalNormalOffsetRight(float value)
        {
            _mainRight.colliderAdditionalNormalOffset = value;
            _outerRight.colliderAdditionalNormalOffset = value;
            _areolaRight.colliderAdditionalNormalOffset = value;
            _nippleRight.colliderAdditionalNormalOffset = value;
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
        private static void SyncGroupSpringMultiplier(float value, float combinedSpring, DAZPhysicsMeshSoftVerticesGroup group)
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
        private static void SyncGroupDamperMultiplier(float value, float combinedDamper, DAZPhysicsMeshSoftVerticesGroup group)
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

        public void ReverseSyncSoftPhysicsOn()
        {
            if(softPhysicsOn.val != _breastPhysicsMesh.on)
            {
                softPhysicsOn.val = _breastPhysicsMesh.on;
            }
        }

        public void ReverseSyncSyncAllowSelfCollision()
        {
            if(allowSelfCollision.val != _breastPhysicsMesh.allowSelfCollision)
            {
                allowSelfCollision.val = _breastPhysicsMesh.allowSelfCollision;
            }
        }

        private void SyncSoftPhysicsOn(bool value)
        {
            _breastPhysicsMesh.on = value;
        }

        private void SyncAllowSelfCollision(bool value)
        {
            _breastPhysicsMesh.allowSelfCollision = value;
        }

        public void UpdatePhysics(
            float massAmount,
            float realMassAmount,
            float softnessAmount,
            float quicknessAmount
        )
        {
            leftBreastParameters.Values
                .Concat(leftBreastGroupParameters.Values)
                .Concat(rightBreastParameters.Values)
                .Concat(rightBreastGroupParameters.Values)
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
            leftBreastParameters.Values
                .Concat(leftBreastGroupParameters.Values)
                .Concat(rightBreastParameters.Values)
                .Concat(rightBreastGroupParameters.Values)
                .Where(param => param.config != null && param.config.dependOnPhysicsRate).ToList()
                .ForEach(param => UpdateParam(param, massAmount, realMassAmount, softnessAmount, quicknessAmount));
        }

        public void UpdateNipplePhysics(float massAmount, float softnessAmount, float nippleErection)
        {
            leftNippleGroupParameters.Values
                .Concat(rightNippleGroupParameters.Values)
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
            _originalSoftPhysicsOn = _breastPhysicsMesh.on;
            SyncSoftPhysicsOn(softPhysicsOn.val);

            _originalAllowSelfCollision = _breastPhysicsMesh.allowSelfCollision;
            SyncAllowSelfCollision(allowSelfCollision.val);

            // auto fat collider radius off (no effect)
            _originalAutoFatColliderRadius = _breastPhysicsMesh.softVerticesUseAutoColliderRadius;
            _breastPhysicsMesh.softVerticesUseAutoColliderRadius = false;

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
            _breastPhysicsMesh.on = _originalSoftPhysicsOn;
            _breastPhysicsMesh.allowSelfCollision = _originalAllowSelfCollision;
            _breastPhysicsMesh.softVerticesUseAutoColliderRadius = _originalAutoFatColliderRadius;

            foreach(string name in _breastPhysicsMeshFloatParamNames)
            {
                _breastPhysicsMesh.GetFloatJSONParam(name).val = _originalBreastPhysicsMeshFloats[name];
            }

            foreach(var group in _breastPhysicsMesh.softVerticesGroups)
            {
                group.useParentSettings = _originalGroupsUseParentSettings[group.name];
            }
        }

        public JSONClass Serialize()
        {
            var jsonClass = new JSONClass();
            jsonClass[SOFT_PHYSICS_ON].AsBool = _originalSoftPhysicsOn;
            jsonClass[ALLOW_SELF_COLLISION].AsBool = _originalAllowSelfCollision;
            jsonClass["breastPhysicsMeshFloats"] = JSONArrayFromDictionary(_originalBreastPhysicsMeshFloats);
            jsonClass[SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS].AsBool = _originalAutoFatColliderRadius;
            jsonClass["groupsUseParentSettings"] = JSONArrayFromDictionary(_originalGroupsUseParentSettings);
            return jsonClass;
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

        public void RestoreFromJSON(JSONClass originalJson)
        {
            _originalSoftPhysicsOn = originalJson[SOFT_PHYSICS_ON].AsBool;
            _originalAllowSelfCollision = originalJson[ALLOW_SELF_COLLISION].AsBool;
            _originalAutoFatColliderRadius = originalJson[SOFT_VERTICES_USE_AUTO_COLLIDER_RADIUS].AsBool;

            var breastPhysicsMeshFloats = originalJson["breastPhysicsMeshFloats"].AsArray;
            foreach(JSONClass json in breastPhysicsMeshFloats)
            {
                _originalBreastPhysicsMeshFloats[json["paramName"].Value] = json["value"].AsFloat;
            }

            var groupsUseParentSettings = originalJson["groupsUseParentSettings"].AsArray;
            foreach(JSONClass json in groupsUseParentSettings)
            {
                _originalGroupsUseParentSettings[json["paramName"].Value] = json["value"].AsBool;
            }
        }

        private static Dictionary<string, string> CreateInfoTexts()
        {
            var texts = new Dictionary<string, string>();

            texts[SOFT_VERTICES_COMBINED_SPRING] =
                $"";

            texts[SOFT_VERTICES_COMBINED_DAMPER] =
                $"";

            texts[SOFT_VERTICES_MASS] =
                $"";

            texts[SOFT_VERTICES_BACK_FORCE] =
                $"";

            texts[SOFT_VERTICES_BACK_FORCE_THRESHOLD_DISTANCE] =
                $"";

            texts[SOFT_VERTICES_BACK_FORCE_MAX_FORCE] =
                $"";

            texts[SOFT_VERTICES_COLLIDER_RADIUS] =
                $"";

            texts[SOFT_VERTICES_COLLIDER_ADDITIONAL_NORMAL_OFFSET] =
                $"";

            texts[SOFT_VERTICES_DISTANCE_LIMIT] =
                $"";

            return texts;
        }
    }
}
