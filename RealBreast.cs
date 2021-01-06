using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace everlaster
{
    // The basic idea is from BreastAutoGravity v2.0 by VeeRifter
    // https://hub.virtamate.com/resources/breastautogravity.662/
    public class RealBreast : MVRScript
    {
        const string pluginName = "RealBreast";
        const string pluginVersion = "1.0.0";
        private Transform chest;
        private List<MorphConfig> morphs = new List<MorphConfig>();

        //storables
        protected JSONStorableFloat softness;

        //DebugInfo storables
        protected JSONStorableString rollAndPitchInfo = new JSONStorableString("Roll And Pitch Info", "");
        protected JSONStorableString morphInfo = new JSONStorableString("Morph Info", "");

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "Person")
                {
                    SuperController.LogError($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
                    return;
                }

                chest = containingAtom.GetStorableByID("chest").transform;
                CreateVersionInfoField();
                InitUI();
                InitMorphs();
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void CreateVersionInfoField()
        {
            JSONStorableString jsonString = new JSONStorableString("VersionInfo", $"{pluginName} {pluginVersion}");
            UIDynamicTextField textField = CreateTextField(jsonString, false);
            textField.UItext.fontSize = 40;
            textField.height = 100;
        }

        // TODO Zero all BuiltIn breast morphs
        // TODO Load reference XS, S, M, L, XL, XXL breasts
        void InitUI()
        {
            softness = CreateFloatSlider("Softness", 1f, 0f, 1.5f);

            //DebugInfo fields
            UIDynamicTextField infoField = CreateTextField(rollAndPitchInfo, false);
            infoField.height = 100;
            UIDynamicTextField infoField2 = CreateTextField(morphInfo, true);
            infoField2.height = 1000;
            infoField2.UItext.fontSize = 26;
        }

        JSONStorableFloat CreateFloatSlider(string paramName, float startingValue, float minValue, float maxValue)
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            RegisterFloat(storable);
            CreateSlider(storable, false);
            return storable;
        }

        void InitMorphs()
        {
            JSONStorable js = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector dcs = js as DAZCharacterSelector;
            GlobalVar.MORPH_UI = dcs.morphsControlUI;

            // TODO lock left/right and up/down etc. control pose morphs
            // TODO test size/shape morph so it can be zeroed when leaning forward/back -> compensate with neutral size morph

            // UPRIGHT and UPSIDE_DOWN adjustments for initial "Zero G" breast morphs
            morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Breast Move Up Right", new Dictionary<string, float> {
                    { Types.UPRIGHT, -0.75f },
                    { Types.UPSIDE_DOWN, 0.20f },
                }),
                new MorphConfig("Breast Move Up Left", new Dictionary<string, float> {
                    { Types.UPRIGHT, -0.75f },
                    { Types.UPSIDE_DOWN, 0.20f },
                }),
                new MorphConfig("Breast Under Smoother1 (Pose)", new Dictionary<string, float> { // Not softness adjustable
                    { Types.UPRIGHT, -0.10f },
                    { Types.UPSIDE_DOWN, 0.05f },
                }),
                new MorphConfig("Breast Under Smoother3 (Pose)", new Dictionary<string, float> { // Not softness adjustable
                    { Types.UPRIGHT, -0.50f },
                    { Types.UPSIDE_DOWN, 0.25f },
                }),
                new MorphConfig("Breasts Natural (Pose)", new Dictionary<string, float> {
                    { Types.UPRIGHT, 0.50f },
                }),
                new MorphConfig("Breast Diameter (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.35f },
                }),
                new MorphConfig("Breasts Implants (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.30f },
                }),
            });

            // LEAN BACK and LEAN FORWARD adjustments for initial "Zero G" breast morphs
            morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Breast Diameter (Pose, Copy)", new Dictionary<string, float> {
                    { Types.LEAN_BACK, 0.35f },
                    { Types.LEAN_FORWARD, -0.20f },
                }),
                new MorphConfig("Breasts Implants (Pose, Copy)", new Dictionary<string, float> {
                    { Types.LEAN_BACK, -0.30f },
                }),
            });

            // other UPSIDE_DOWN morphs
            morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Areola UpDown", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -1.00f },
                }),
                new MorphConfig("Center Gap Depth (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.10f },
                }),
                new MorphConfig("Center Gap Height (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.20f },
                }),
                new MorphConfig("Center Gap UpDown (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.20f },
                }),
                new MorphConfig("Chest Smoother (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.50f },
                }),
                new MorphConfig("ChestUnderBreast (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.50f },
                }),
                new MorphConfig("ChestUp (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.10f },
                }),
                new MorphConfig("Breast Rotate Up Left", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.20f },
                }),
                new MorphConfig("Breast Rotate Up Right", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.20f },
                }),
                new MorphConfig("Breast Sag1 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.15f },
                }),
                new MorphConfig("Breast Sag2 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.50f },
                }),
                new MorphConfig("Breast Top Curve1 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.50f },
                }),
                new MorphConfig("Breast Top Curve2 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.50f },
                }),
                new MorphConfig("Breasts Flatten", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.30f },
                }),
                new MorphConfig("Breast flat (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.3f },
                }),
                new MorphConfig("Breasts Hang Forward", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.30f },
                }),
                new MorphConfig("Breasts TogetherApart", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.50f },
                }),
                new MorphConfig("Breasts Upward Slope (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 1.00f },
                }),
                new MorphConfig("BreastsShape2 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.75f },
                }),
                new MorphConfig("Sternum Height (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.30f },
                }),
            });

            // other LEAN_BACK and LEAN_FORWARD morphs
            morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Areola S2S Op", new Dictionary<string, float> {
                    { Types.LEAN_BACK, 1.00f },
                }),
                new MorphConfig("Breast Side Smoother (Pose)", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.25f },
                }),
                new MorphConfig("Breasts Depth", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.45f },
                    { Types.LEAN_BACK, -0.25f },
                }),
                new MorphConfig("Breasts Flatten (Copy)", new Dictionary<string, float> {
                    { Types.LEAN_BACK, 0.50f },
                }),
                new MorphConfig("Breasts Height", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, -0.20f },
                }),
                new MorphConfig("Breasts Hang Forward (Copy)", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.10f },
                }),
                new MorphConfig("Breasts Move S2S Op", new Dictionary<string, float> {
                    { Types.LEAN_BACK, -0.20f },
                }),
                new MorphConfig("Breasts TogetherApart (Copy)", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.40f },
                }),
            });

            // ROLL_LEFT and ROLL_RIGHT morphs
            morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Aerola S2S Left (Copy)", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, -0.75f },
                }),
                new MorphConfig("Aerola S2S Right (Copy)", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, -0.75f },
                }),
                new MorphConfig("Breast Move S2S In Left (Copy)", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.15f },
                }),
                new MorphConfig("Breast Move S2S In Right (Copy)", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.15f },
                }),
                new MorphConfig("Breast Move S2S Out Left (Copy)", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.45f },
                }),
                new MorphConfig("Breast Move S2S Out Right (Copy)", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.45f },
                }),
                new MorphConfig("Breast Rotate X In Left", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.45f },
                }),
                new MorphConfig("Breast Rotate X In Right", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.45f },
                }),
                new MorphConfig("Breasts Diameter (Pose)", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.15f },
                    { Types.ROLL_RIGHT, 0.15f },
                }),
                new MorphConfig("Breasts Implants Left (Pose)", new Dictionary<string, float> { 
                    { Types.ROLL_LEFT, -0.20f },
                }),
                new MorphConfig("Breasts Implants Right (Pose)", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, -0.20f },
                }),
                new MorphConfig("Breasts Move S2S Dir", new Dictionary<string, float> { // max 1! 0.75?
                    { Types.ROLL_LEFT, 0.60f },
                    { Types.ROLL_RIGHT, -0.60f },
                }),
                new MorphConfig("Breasts Width", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, -0.15f },
                    { Types.ROLL_RIGHT, -0.15f },
                }),
                new MorphConfig("Centre Gap Narrow (Pose)", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.25f },
                    { Types.ROLL_RIGHT, 0.25f },
                }),
                new MorphConfig("Center Gap Smooth (Pose)", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.30f },
                    { Types.ROLL_RIGHT, 0.30f },
                }),
            });
        }

        public void Update()
        {
            Quaternion q = chest.rotation;
            float roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
            float pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);

            AdjustForRoll(roll, softness.val);

            // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
            // -> if person is sideways, pitch related morphs have less effect
            AdjustForPitch(pitch, softness.val * (90 - Mathf.Abs(roll)) / 90);

            DebugInfo(pitch, roll);
        }

        void AdjustForRoll(float roll, float effect)
        {
            // left
            if(roll >= 0)
            {
                ZeroMorphs(Types.ROLL_RIGHT);
                AdjustMorphs(Types.ROLL_LEFT, Remap(roll, effect));
            }
            // right
            else
            {
                ZeroMorphs(Types.ROLL_LEFT);
                AdjustMorphs(Types.ROLL_RIGHT, Remap(Mathf.Abs(roll), effect));
            }
        }

        void AdjustForPitch(float pitch, float effect)
        {
            // leaning forward
            if (pitch > 0)
            {
                ZeroMorphs(Types.LEAN_BACK);
                // upright
                if(pitch <= 90)
                {
                    ZeroMorphs(Types.UPSIDE_DOWN);
                    AdjustMorphs(Types.LEAN_FORWARD, Remap(pitch, effect));
                    AdjustMorphs(Types.UPRIGHT, Remap(90 - pitch, effect));
                }
                // upside down
                else
                {
                    ZeroMorphs(Types.UPRIGHT);
                    AdjustMorphs(Types.LEAN_FORWARD, Remap(180 - pitch, effect));
                    AdjustMorphs(Types.UPSIDE_DOWN, Remap(pitch - 90, effect));
                }
            }
            // leaning back
            else
            {
                ZeroMorphs(Types.LEAN_FORWARD);
                // upright
                if(pitch > -90)
                {
                    ZeroMorphs(Types.UPSIDE_DOWN);
                    AdjustMorphs(Types.LEAN_BACK, Remap(Mathf.Abs(pitch), effect));
                    AdjustMorphs(Types.UPRIGHT, Remap(90 - Mathf.Abs(pitch), effect));
                }
                // upside down
                else
                {
                    ZeroMorphs(Types.UPRIGHT);
                    AdjustMorphs(Types.LEAN_BACK, Remap(180 - Mathf.Abs(pitch), effect));
                    AdjustMorphs(Types.UPSIDE_DOWN, Remap(Mathf.Abs(pitch) - 90, effect));
                }
            }
        }

        void ZeroMorphs(string type)
        {
            foreach(var it in morphs)
            {
                if(it.Effects.ContainsKey(type) && it.Effects.Count == 1)
                {
                    it.Morph.morphValue = 0;
                }
            }
        }

        void AdjustMorphs(string type, float effect)
        {
            foreach(var it in morphs)
            {
                if(it.Effects.ContainsKey(type))
                {
                    it.Morph.morphValue = it.Effects[type] * effect;
                }
            }
        }

        float Remap(float angle, float effect)
        {
            return angle * effect / 90;
        }

        void OnDestroy()
        {
            foreach(var it in morphs)
            {
                it.Morph.morphValue = 0;
            }
        }

        void DebugInfo(float pitch, float roll)
        {
            rollAndPitchInfo.SetVal($"Pitch: {pitch}\r\nRoll: {roll}");

            string morphInfoText = "";
            foreach(var it in morphs)
            {
                morphInfoText += it.ToString();
            }
            morphInfo.SetVal(morphInfoText);
        }
    }

    public static class GlobalVar
    {
        public static GenerateDAZMorphsControlUI MORPH_UI { get; set; }
    }

    public static class Types
    {
        public const string LEAN_FORWARD = "leanForward";
        public const string LEAN_BACK = "leanBack";
        public const string UPSIDE_DOWN = "upsideDown";
        public const string ROLL_RIGHT = "rollRight";
        public const string ROLL_LEFT = "rollLeft";
        public const string UPRIGHT = "upright";
        public const string SAG_ADJUST = "sagAdjust";
    }

    class MorphConfig
    {
        public string Name { get; set; }
        public DAZMorph Morph { get; set; }
        public Dictionary<string, float> Effects { get; set; }

        public MorphConfig(string name, Dictionary<string, float> effects)
        {
            Name = name;
            Morph = GlobalVar.MORPH_UI.GetMorphByDisplayName(name);
            Effects = effects;
        }

        public string ToString()
        {
            float value = (float) Math.Round(this.Morph.morphValue * 1000f) / 1000f;
            if(value != 0)
            {
                return this.Name + ":  " + value + "\r\n";
            }

            return "";
        }
    }
}
