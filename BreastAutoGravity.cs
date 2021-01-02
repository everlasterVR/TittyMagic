using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace everlaster
{
    public class BreastAutoGravity : MVRScript
    {
		protected string versionText = "BreastAutoGravity v2.0 - VeeRifter";
		private Transform chest;
		private GenerateDAZMorphsControlUI morphUI;

		// morphs
		private DAZMorph forwardBreastDroop;
		private DAZMorph breastsTogetherApart;
		private DAZMorph breastSag2;
		private DAZMorph breastsCenterShift;

		// max value storables
		protected JSONStorableFloat forwardDroop;
		protected JSONStorableFloat forwardCleavage;
		protected JSONStorableFloat reclineSpread;
		protected JSONStorableFloat reclineFlatten;
		protected JSONStorableFloat invertedSag;
		protected JSONStorableFloat sidewaysRoll;

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
				InitUI();
				InitMorphs();
				//VersionInfo();
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
		}

		void InitUI()
		{
			JSONStorable js = containingAtom.GetStorableByID("geometry");
			DAZCharacterSelector dcs = js as DAZCharacterSelector;
			morphUI = dcs.morphsControlUI;
			forwardDroop = CreateFloatSlider("Forward Droop", 0.5f, 0f, 1.5f);
			forwardCleavage = CreateFloatSlider("Forward Cleavage", 0.5f, -0.5f, 1f);
			reclineSpread = CreateFloatSlider("Recline Spread", 0.5f, 0f, 1f);
			reclineFlatten = CreateFloatSlider("Recline Flatten", 0.15f, 0f, 0.3f);
			invertedSag = CreateFloatSlider("Inverted Sag", 1.5f, 0f, 3f);
			sidewaysRoll = CreateFloatSlider("Sideways Roll", 0.5f, 0f, 1f);
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
			forwardBreastDroop = morphUI.GetMorphByDisplayName("Forward Breast Droop");
			breastsTogetherApart = morphUI.GetMorphByDisplayName("Breasts TogetherApart");
			breastsCenterShift = morphUI.GetMorphByDisplayName("Breasts Center Shift");
			breastSag2 = morphUI.GetMorphByDisplayName("Breast Sag2");
		}

		void VersionInfo()
		{
			JSONStorableString jsText = new JSONStorableString("Version", versionText);
			UIDynamicTextField dtf = CreateTextField(jsText, false);
			dtf.height = 115;
		}

		public void Update()
        {
			Quaternion q = chest.rotation;
			
			float pitch = Mathf.Rad2Deg * Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
			if(pitch >= 0)
			{
				OnLeanForward(pitch);
			}
			else
			{
				OnLeanBack(pitch);
			}

			float roll = Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
			if(roll >= 0)
			{
				OnRollLeft(roll);
			}
			else
			{
				OnRollRight(roll);
			}
		}

		void OnLeanForward(float pitch)
		{
			if(pitch <= 90)
			{
				forwardBreastDroop.morphValue = Remap(pitch, 0, 90, 0, forwardDroop.val);
				breastsTogetherApart.morphValue = Remap(pitch, 0, 90, 0, forwardCleavage.val);
				breastSag2.morphValue = 0;
			}
			//inverted face down
			else
			{
				forwardBreastDroop.morphValue = Remap(180 - pitch, 0, 90, 0, forwardDroop.val);
				breastsTogetherApart.morphValue = Remap(180 - pitch, 0, 90, 0, forwardCleavage.val);
				breastSag2.morphValue = Remap(180 - pitch, 0, 90, -invertedSag.val, 0);
			}
		}

		void OnLeanBack(float pitch)
		{
			if(pitch > -90)
			{
				forwardBreastDroop.morphValue = Remap(Mathf.Abs(pitch), 0, 90, 0, -reclineFlatten.val);
				breastsTogetherApart.morphValue = Remap(Mathf.Abs(pitch), 0, 90, 0, -reclineSpread.val);
				breastSag2.morphValue = 0;
			}
			//inverted face up
			else
			{
				forwardBreastDroop.morphValue = Remap(180 - Mathf.Abs(pitch), 0, 90, 0, -reclineFlatten.val);
				breastsTogetherApart.morphValue = Remap(180 - Mathf.Abs(pitch), 0, 90, 0, -reclineSpread.val);
				breastSag2.morphValue = Remap(180 - Mathf.Abs(pitch), 0, 90, -invertedSag.val, 0);
			}
		}

		void OnRollLeft(float roll)
		{
			//face up
			if(roll <= 90)
			{
				breastsCenterShift.morphValue = Remap(roll, 0, 90, 0, sidewaysRoll.val);
			}
			//face down
			else
			{
				breastsCenterShift.morphValue = Remap(180 - roll, 0, 90, 0, sidewaysRoll.val);
			}
		}

		void OnRollRight(float roll)
		{
			//face up
			if(roll > -90)
			{
				breastsCenterShift.morphValue = Remap(Mathf.Abs(roll), 0, 90, 0, -sidewaysRoll.val);
			}
			//face down
			else
			{
				breastsCenterShift.morphValue = Remap(180 - Mathf.Abs(roll), 0, 90, 0, -sidewaysRoll.val);
			}
		}

		float Remap(float inVal, float min1, float max1, float min2, float max2)
		{
			var ratio = (max2 - min2) / (max1 - min1);
			var c = min2 - ratio * min1;
			return ratio * inVal + c;
		}
	}
}
