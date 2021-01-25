using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    class GravityMorphHandler
    {
        private List<GravityMorphConfig> morphs;

        private float roll;
        private float pitch;
        private float scale;
        private float softness;
        private float sag;

        public GravityMorphHandler()
        {
            morphs = new List<GravityMorphConfig>();
            InitGravityMorphs();
            morphs.ForEach(it => it.Reset());
        }

        public void Update(
            float roll,
            float pitch,
            float scale,
            float softness,
            float sag
        )
        {
            this.roll = roll;
            this.pitch = pitch;
            this.scale = scale;
            this.softness = softness;
            this.sag = sag;

            AdjustMorphsForRoll();
            AdjustMorphsForPitch(Calc.RollFactor(roll));
        }

        public void Reset(string type = "")
        {
            morphs
                .Where(it => type == "" || (it.Multipliers.ContainsKey(type) && it.Multipliers.Count == 1))
                .ToList().ForEach(it => it.Reset());
        }

        public string GetStatus()
        {
            string text = "";
            morphs.ForEach((it) =>
            {
                text = text + Formatting.NameValueString(it.Name, it.Morph.morphValue, 1000f, 30) + "\n";
            });
            return text;
        }

        // TODO refactor to not use Dictionary -> same morph can be listed multiple times for different angle types
        // Possible to merge to one morph per angle type?
        private void InitGravityMorphs()
        {
            morphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                //    Main sag morphs
                new GravityMorphConfig("TM_Breast Move Up", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        { -0.07f,     1.67f,     0.33f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.07f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_Breast Sag1", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.03f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("TM_Breast Sag2", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.05f,     1.25f,     0.75f } },
                }),
                new GravityMorphConfig("TM_Breasts Hang Forward", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.80f } },
                }),
                new GravityMorphConfig("TM_Breasts Natural", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        {  0.08f,     2.00f,     0.00f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.04f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breasts TogetherApart", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.50f,     0.80f } },
                }),

                //    Tweak morphs
                new GravityMorphConfig("TM_Areola UpDown", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.15f,     1.33f,     0.67f } },
                }),
                new GravityMorphConfig("TM_Center Gap Depth", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Center Gap Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Center Gap UpDown", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     0.50f,     1.50f } },
                }),
                new GravityMorphConfig("TM_Chest Smoother", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     0.75f,     1.25f } },
                }),
                new GravityMorphConfig("TM_ChestUnderBreast", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.15f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_ChestUp", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_ChestUpperNarrow", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast flat", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Pointed", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.33f,     0.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate Up", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        {  0.15f,     0.80f,     1.20f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.25f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Top Curve1", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.04f,     2.00f,    -0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Top Curve2", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.06f,     2.00f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother1", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        { -0.04f,     0.50f,     1.50f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.45f,     0.60f,     1.40f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother3", new Dictionary<string, float?[]> {
                    { AngleTypes.UPRIGHT, new float?[]        { -0.08f,     1.00f,     1.00f } },
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.20f,     1.00f,    -1.00f } },
                }),
                new GravityMorphConfig("TM_Breasts Flatten", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.40f,     0.60f } },
                }),
                new GravityMorphConfig("TM_Breasts Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.10f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Implants", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Upward Slope", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.15f,     1.20f,     0.80f } },
                }),
                new GravityMorphConfig("TM_BreastsShape2", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    {  0.50f,     0.67f,     1.33f } },
                }),
                new GravityMorphConfig("TM_Sternum Height", new Dictionary<string, float?[]> {
                    { AngleTypes.UPSIDE_DOWN, new float?[]    { -0.30f,     null,     null } },
                }),
            });

            morphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                new GravityMorphConfig("TM_Breast Depth Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Squash Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Depth Squash Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.20f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.22f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Diameter (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                    { AngleTypes.LEAN_FORWARD, new float?[]   { -0.04f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Large", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.08f,     1.50f,     0.50f } },
                    { AngleTypes.LEAN_FORWARD, new float?[]   { -0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Side Smoother", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.20f,     1.80f,     0.20f } },
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.33f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother1 (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.04f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Under Smoother3 (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.10f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Left", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Right", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.08f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Flatten (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.25f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_Breasts Height (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   { -0.18f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts Hang Forward (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.05f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breasts TogetherApart (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.20f,     1.75f,     0.25f  } },
                }),
                new GravityMorphConfig("TM_Chest Smoother (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.33f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_ChestShape", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      { -0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_ChestSmoothCenter", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.15f,     1.67f,     0.33f } },
                }),
                new GravityMorphConfig("TM_ChestUp (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.20f,     1.00f,     1.00f } },
                }),
                new GravityMorphConfig("TM_Sternum Width", new Dictionary<string, float?[]> {
                    { AngleTypes.LEAN_FORWARD, new float?[]   {  0.25f,     1.25f,     0.75f  } },
                    { AngleTypes.LEAN_BACK, new float?[]      {  0.33f,    -0.67f,     1.33f } },
                }),
            });

            morphs.AddRange(new List<GravityMorphConfig>
            {
                //    USAGE: AdjustMorphs function
                //    angle type                            base       softness   scale
                new GravityMorphConfig("TM_Areola S2S Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      { -0.40f,     1.50f,     0.50f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Areola S2S Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.40f,     1.50f,     0.50f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     { -0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S In Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.28f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S In Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.28f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Left (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Move S2S Out Right (Copy)", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.40f,     1.50f,     0.50f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate X In Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Rotate X In Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.10f,     2.00f,     0.00f } },
                }),
                new GravityMorphConfig("TM_Breast Width Left", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      { -0.03f,     1.60f,     0.40f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.07f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Breast Width Right", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.07f,     1.60f,     0.40f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     { -0.03f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Breasts Diameter", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      { -0.05f,     1.60f,     0.40f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     { -0.05f,     1.60f,     0.40f } },
                }),
                new GravityMorphConfig("TM_Centre Gap Narrow", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.10f,     1.75f,     0.25f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.10f,     1.75f,     0.25f } },
                }),
                new GravityMorphConfig("TM_Center Gap Smooth", new Dictionary<string, float?[]> {
                    { AngleTypes.ROLL_LEFT, new float?[]      {  0.20f,     1.75f,     0.25f } },
                    { AngleTypes.ROLL_RIGHT, new float?[]     {  0.20f,     1.75f,     0.25f } },
                }),
            });
        }

        private void AdjustMorphsForRoll()
        {
            // left
            if(roll >= 0)
            {
                Reset(AngleTypes.ROLL_RIGHT);
                DoAdjustForRoll(AngleTypes.ROLL_LEFT, Calc.Remap(roll, 1));
            }
            // right
            else
            {
                Reset(AngleTypes.ROLL_LEFT);
                DoAdjustForRoll(AngleTypes.ROLL_RIGHT, Calc.Remap(Mathf.Abs(roll), 1));
            }
        }

        private void DoAdjustForRoll(string type, float effect)
        {
            morphs
                .Where(it => it.Multipliers.ContainsKey(type))
                .ToList().ForEach(it => it.UpdateRollVal(type, effect, scale, softness, sag));
        }

        private void AdjustMorphsForPitch(float rollFactor)
        {
            // leaning forward
            if(pitch > 0)
            {
                Reset(AngleTypes.LEAN_BACK);
                // upright
                if(pitch <= 90)
                {
                    Reset(AngleTypes.UPSIDE_DOWN);
                    DoAdjustForPitch(AngleTypes.LEAN_FORWARD, Calc.Remap(pitch, rollFactor));
                    DoAdjustForPitch(AngleTypes.UPRIGHT, Calc.Remap(90 - pitch, rollFactor));
                }
                // upside down
                else
                {
                    Reset(AngleTypes.UPRIGHT);
                    DoAdjustForPitch(AngleTypes.LEAN_FORWARD, Calc.Remap(180 - pitch, rollFactor));
                    DoAdjustForPitch(AngleTypes.UPSIDE_DOWN, Calc.Remap(pitch - 90, rollFactor));
                }
            }
            // leaning back
            else
            {
                Reset(AngleTypes.LEAN_FORWARD);
                // upright
                if(pitch > -90)
                {
                    Reset(AngleTypes.UPSIDE_DOWN);
                    DoAdjustForPitch(AngleTypes.LEAN_BACK, Calc.Remap(Mathf.Abs(pitch), rollFactor));
                    DoAdjustForPitch(AngleTypes.UPRIGHT, Calc.Remap(90 - Mathf.Abs(pitch), rollFactor));
                }
                // upside down
                else
                {
                    Reset(AngleTypes.UPRIGHT);
                    DoAdjustForPitch(AngleTypes.LEAN_BACK, Calc.Remap(180 - Mathf.Abs(pitch), rollFactor));
                    DoAdjustForPitch(AngleTypes.UPSIDE_DOWN, Calc.Remap(Mathf.Abs(pitch) - 90, rollFactor));
                }
            }
        }

        private void DoAdjustForPitch(string type, float effect)
        {
            morphs
                .Where(it => it.Multipliers.ContainsKey(type))
                .ToList().ForEach(it => it.UpdatePitchVal(type, effect, scale, softness, sag));
        }
    }
}
