using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace VeeRifter
{
    public class BreastAutoGravity : MVRScript
    {
		protected string versionText = "BreastAutoGravity v2.0 - VeeRifter";
		private Transform chest;					
		private DAZMorph breastHang;
		private DAZMorph breastSpread;
		private DAZMorph breastSag;
		private DAZMorph breastRoll;		
		protected JSONStorableFloat breastHangMax;
		protected JSONStorableFloat breastCleavageMax;
		protected JSONStorableFloat breastSpreadMax;
		protected JSONStorableFloat breastFlattenMax;
		protected JSONStorableFloat breastSagMax;
		protected JSONStorableFloat breastRollMax;
		
        public override void Init()
        {
            try
            {
				if (containingAtom.type != "Person")
				{
					SuperController.LogError($"Plugin is for use with 'Person' atom, not '{containingAtom.type}'");
					return;
				}
				
                JSONStorable js = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector dcs = js as DAZCharacterSelector;
                GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;
                breastHang = morphUI.GetMorphByDisplayName("Forward Breast Droop");
                breastSpread = morphUI.GetMorphByDisplayName("Breasts TogetherApart");
                breastRoll = morphUI.GetMorphByDisplayName("Breasts Center Shift");
                breastSag = morphUI.GetMorphByDisplayName("Breast Sag2");
				chest = containingAtom.GetStorableByID("chest").transform;				

				breastHangMax = new JSONStorableFloat("Forward Breast Droop", 0.5f, 0f, 1.5f, true, true);
				breastHangMax.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(breastHangMax);
				CreateSlider(breastHangMax, false);

				breastCleavageMax = new JSONStorableFloat("Forward Breast Cleavage", 0.5f, -0.5f, 1f, true, true);
				breastCleavageMax.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(breastCleavageMax);
				CreateSlider(breastCleavageMax, true);

				breastSpreadMax = new JSONStorableFloat("Recline Breast Spread", 0.5f, 0f, 1f, true, true);
				breastSpreadMax.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(breastSpreadMax);
				CreateSlider(breastSpreadMax, false);

				breastFlattenMax = new JSONStorableFloat("Recline Breast Flatten", 0.15f, 0f, 0.3f, true, true);
				breastSpreadMax.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(breastFlattenMax);
				CreateSlider(breastFlattenMax, true);

				breastSagMax = new JSONStorableFloat("Inverted Breast Sag", 1.5f, 0f, 3f, true, true);
				breastSagMax.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(breastSagMax);
				CreateSlider(breastSagMax, false);				

				breastRollMax = new JSONStorableFloat("Sideways Breast Roll", 0.5f, 0f, 1f, true, true);
				breastRollMax.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(breastRollMax);
				CreateSlider(breastRollMax, true);

				JSONStorableString jsText = new JSONStorableString("Version", versionText);
				UIDynamicTextField dtf = CreateTextField(jsText, false);
				dtf.height = 115;
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        public void Update()
        {
			Quaternion q = chest.rotation;
			
			float Pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
			float Roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);			
			
			//Leaning forward
			if (Pitch >= 0)
			{
				if (Pitch <= 90)
				{
					breastHang.morphValue = Remap(Pitch, 0, 90, 0, breastHangMax.val);
					breastSpread.morphValue = Remap(Pitch, 0, 90, 0, breastCleavageMax.val);
					breastSag.morphValue = 0;
				}
				//Inverted face down
				else
				{
					breastHang.morphValue = Remap(180 - Pitch, 0, 90, 0, breastHangMax.val);
					breastSpread.morphValue = Remap(180 - Pitch, 0, 90, 0, breastCleavageMax.val);
					breastSag.morphValue = Remap(180 - Pitch, 0, 90, -breastSagMax.val, 0);					
				}
			}
			//Leaning back
			else
			{
				if (Pitch > -90)
				{
					breastHang.morphValue = Remap(Mathf.Abs(Pitch), 0, 90, 0, -breastFlattenMax.val);
					breastSpread.morphValue = Remap(Mathf.Abs(Pitch), 0, 90, 0, -breastSpreadMax.val);
					breastSag.morphValue = 0;
				}
				//Inverted face up
				else
				{
					breastHang.morphValue = Remap(180 - Mathf.Abs(Pitch), 0, 90, 0, -breastFlattenMax.val);
					breastSpread.morphValue = Remap(180 - Mathf.Abs(Pitch), 0, 90, 0, -breastSpreadMax.val);
					breastSag.morphValue = Remap(180 - Mathf.Abs(Pitch), 0, 90, -breastSagMax.val, 0);				
				}
			}
			//Rolling left
			if (Roll >= 0)
			{
				//face up
				if (Roll <= 90)
				{
					breastRoll.morphValue = Remap(Roll, 0, 90, 0, breastRollMax.val);
				}
				//face down
				else
				{
					breastRoll.morphValue = Remap(180 - Roll, 0, 90, 0, breastRollMax.val);					
				}
			}
			//Rolling right
			else
			{
				//face up
				if (Roll > -90)
				{
					breastRoll.morphValue = Remap(Mathf.Abs(Roll), 0, 90, 0, -breastRollMax.val);
				}
				//face down
				else
				{
					breastRoll.morphValue = Remap(180 - Mathf.Abs(Roll), 0, 90, 0, -breastRollMax.val);				
				}
			}
        }
		
        private float Remap(float inVal, float min1, float max1, float min2, float max2)
        {
            var ratio = (max2 - min2) / (max1 - min1);		
            var c = min2 - ratio * min1;			
            return ratio * inVal + c;
        }
    }
}
