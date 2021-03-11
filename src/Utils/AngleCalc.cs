using UnityEngine;

namespace TittyMagic
{
    /*
     * LICENSE: Creative Commons Attribution-ShareAlike 4.0 Generic (CC BY-SA 4.0) https://creativecommons.org/licenses/by-sa/4.0/
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
    }
}
