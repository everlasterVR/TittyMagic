using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace everlaster
{
    // The basic idea and the following implementations are from BreastAutoGravity v2.0 by VeeRifter
    // https://hub.virtamate.com/resources/breastautogravity.662/
    // - get morphs from containingAtom's morphsControlUI
    // - pitch and roll calculation from chest rotation quarternion
    // - Remap function
    public class RealBreast : MVRScript
    {
        const string pluginName = "RealBreast";
        const string pluginVersion = "1.0.0";
        private Transform chest;
        private List<MorphConfig> morphs = new List<MorphConfig>();

        //storables
        protected JSONStorableFloat pitchEffect;
        protected JSONStorableFloat rollEffect;

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

        void InitUI()
        {
            pitchEffect = CreateFloatSlider("Pitch effect", 1f, 0f, 2f);
            rollEffect = CreateFloatSlider("Roll effect", 1f, 0f, 2f);

            //DebugInfo fields
            UIDynamicTextField infoField = CreateTextField(rollAndPitchInfo, false);
            infoField.height = 100;
            UIDynamicTextField infoField2 = CreateTextField(morphInfo, true);
            infoField2.height = 1000;
        }

        JSONStorableFloat CreateFloatSlider(string paramName, float startingValue, float minValue, float maxValue)
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Full;
            RegisterFloat(storable);
            CreateSlider(storable, false);
            return storable;
        }

        void InitMorphs()
        {
            JSONStorable js = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector dcs = js as DAZCharacterSelector;
            GlobalVar.MORPH_UI = dcs.morphsControlUI;

            morphs.AddRange(new List<MorphConfig>
            {
                new MorphConfig("Breasts Natural (Pose)", new Dictionary<string, float> {
                    { Types.UPRIGHT, 0.75f }
                }),
                new MorphConfig("Breasts TogetherApart", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.66f },
                    { Types.LEAN_BACK, -0.66f }
                }),
                new MorphConfig("Breasts Depth", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.33f },
                    { Types.LEAN_BACK, -1f }
                }),
                new MorphConfig("Breasts Push Up 01 (Pose)", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.33f }
                }),
                new MorphConfig("Breasts Hang Forward", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.66f }
                }),
                new MorphConfig("Breasts Height", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, -0.66f }
                }),
                new MorphConfig("Breasts Width", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, -0.66f }
                }),
                new MorphConfig("Centre Gap Wide (Pose)", new Dictionary<string, float> {
                    { Types.LEAN_FORWARD, 0.5f }
                }),
                new MorphConfig("Breasts Flatten", new Dictionary<string, float> {
                    { Types.LEAN_BACK, 0.33f }
                }),
                new MorphConfig("Breast Rotate X Out Right", new Dictionary<string, float> {
                    { Types.LEAN_BACK, 0.33f }
                }),
                new MorphConfig("Breast Rotate X Out Left", new Dictionary<string, float> {
                    { Types.LEAN_BACK, 0.33f }
                }),
                new MorphConfig("Breasts Size (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, -0.2f }
                }),
                new MorphConfig("Breast Move Up Right", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.33f }
                }),
                new MorphConfig("Breast Move Up Left", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.33f }
                }),
                new MorphConfig("Breast Sag1 (Pose)", new Dictionary<string, float> {
                    { Types.SAG_ADJUST, 1f }
                }),
                new MorphConfig("Breast Sag2 (Pose)", new Dictionary<string, float> {
                    { Types.SAG_ADJUST, 2f }
                }),
                new MorphConfig("Breast Side Smoother (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.5f }
                }),
                new MorphConfig("Breast Under Smoother1 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.2f }
                }),
                new MorphConfig("Breast Under Smoother3 (Pose)", new Dictionary<string, float> {
                    { Types.UPSIDE_DOWN, 0.2f }
                }),
                new MorphConfig("Center Gap Smooth (Pose)", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.66f },
                    { Types.ROLL_LEFT, 0.66f }
                }),
                new MorphConfig("Centre Gap Narrow (Pose)", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.66f },
                    { Types.ROLL_LEFT, 0.66f }
                }),
                new MorphConfig("Breast Rotate X In Right", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.66f }
                }),
                new MorphConfig("Breast Rotate X In Left", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.66f }
                }),
                new MorphConfig("Breast Move S2S In Right", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 0.33f }
                }),
                new MorphConfig("Breast Move S2S In Left", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 0.33f }
                }),
                new MorphConfig("Breast Move S2S Out Right", new Dictionary<string, float> {
                    { Types.ROLL_RIGHT, 1f }
                }),
                new MorphConfig("Breast Move S2S Out Left", new Dictionary<string, float> {
                    { Types.ROLL_LEFT, 1f }
                })
            });
        }

        public void Update()
        {
            Quaternion q = chest.rotation;
            float pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
            float roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);

            // Scale pitch effect by roll angle's distance from 90/-90 = person is sideways
            // -> if person is sideways, pitch related morphs have less effect
            float scaledPitchEffect = pitchEffect.val * (90 - Mathf.Abs(roll)) / 90;
            if(pitch >= 0)
            {
                OnLeanForward(pitch, scaledPitchEffect);
            }
            else
            {
                OnLeanBack(pitch, scaledPitchEffect);
            }

            if(roll >= 0)
            {
                ZeroMorphs(Types.ROLL_RIGHT);
                AdjustMorphs(Types.ROLL_LEFT, Remap(roll, rollEffect.val));
            }
            else
            {
                ZeroMorphs(Types.ROLL_LEFT);
                AdjustMorphs(Types.ROLL_RIGHT, Remap(Mathf.Abs(roll), rollEffect.val));
            }

            DebugInfo(pitch, roll);
        }

        void OnLeanForward(float pitch, float effect)
        {
            ZeroMorphs(Types.LEAN_BACK);

            float inVal = pitch <= 90 ? pitch : 180 - pitch;
            AdjustMorphs(Types.LEAN_FORWARD, Remap(inVal, effect));
            AdjustMorphs(Types.UPRIGHT, Remap(90 - pitch, effect));

            // Right side up
            if(pitch <= 90)
            {
                ZeroMorphs(Types.UPSIDE_DOWN);
                ZeroMorphs(Types.SAG_ADJUST);
            }
            // Upside down
            else
            {
                AdjustMorphs(Types.UPSIDE_DOWN, Remap(180 - Mathf.Abs(pitch), 0, 90, effect, 0));
                AdjustMorphs(Types.SAG_ADJUST, Remap(180 - pitch, -effect, 0));
            }
        }

        void OnLeanBack(float pitch, float effect)
        {
            ZeroMorphs(Types.LEAN_FORWARD);

            float inVal = pitch > -90 ? Mathf.Abs(pitch) : 180 - Mathf.Abs(pitch);
            AdjustMorphs(Types.LEAN_BACK, Remap(inVal, effect));
            AdjustMorphs(Types.UPRIGHT, Remap(90 - Mathf.Abs(pitch), effect));

            // Leaning back slightly
            if(pitch > -45)
            {
                ZeroMorphs(Types.SAG_ADJUST);
            }
            // Almost horizontal
            else
            {
                AdjustMorphs(Types.SAG_ADJUST, Remap(180 - Mathf.Abs(pitch), 0, 135, -effect, 0));
            }

            // Right side up
            if(pitch > -90)
            {
                ZeroMorphs(Types.UPSIDE_DOWN);
            }
            // Upside down
            else
            {
                AdjustMorphs(Types.UPSIDE_DOWN, Remap(180 - Mathf.Abs(pitch), effect, 0));
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

        void AdjustMorphs(string type, float baseVal)
        {
            foreach(var it in morphs)
            {
                if(it.Effects.ContainsKey(type))
                {
                    it.Morph.morphValue = it.Effects[type] * baseVal;
                }
            }
        }

        float Remap(float inVal, float max2)
        {
            return Remap(inVal, 0, 90, 0, max2);
        }

        float Remap(float inVal, float min2, float max2)
        {
            return Remap(inVal, 0, 90, min2, max2);
        }

        float Remap(float inVal, float min1, float max1, float min2, float max2)
        {
            var ratio = (max2 - min2) / (max1 - min1);
            var c = min2 - ratio * min1;
            return ratio * inVal + c;
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
                morphInfoText += "\r\n";
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
            return this.Name + ":  " + value;
        }
    }
}
