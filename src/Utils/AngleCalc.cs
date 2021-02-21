using UnityEngine;

namespace TittyMagic
{
    /*
     * LICENSE: Creative Commons Attribution-ShareAlike 2.0 Generic (CC BY-SA 2.0) https://creativecommons.org/licenses/by-sa/2.0/
     * Adapted from BreastAutoGravity.2 by VeeRifter (CC BY-SA)
     * https://hub.virtamate.com/resources/breastautogravity.662/
     */

    public static class AngleCalc
    {
        public static float Roll(Quaternion q)
        {
            return Mathf.Rad2Deg * Mathf.Asin(2 * q.x * q.y + 2 * q.z * q.w);
        }

        public static float Pitch(Quaternion q)
        {
            return Mathf.Rad2Deg* Mathf.Atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z);
        }

        // This is used to scale pitch effect by roll angle's distance from 90/-90 = person is sideways
        //-> if person is sideways, pitch related morphs have less effect
        public static float RollFactor(float roll)
        {
            return (90 - Mathf.Abs(roll)) / 90;
        }

        public static float Remap(float angle, float effect)
        {
            return angle * effect / 90;
        }
    }
}
