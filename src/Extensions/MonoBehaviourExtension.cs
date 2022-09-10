// ReSharper disable MemberCanBePrivate.Global UnusedMember.Global UnusedMethodReturnValue.Global UnusedType.Global
using UnityEngine;

public static class MonoBehaviourExtension
{
    public static void SetEnabled(this MonoBehaviour monoBehaviour, bool value)
    {
        if(monoBehaviour != null)
        {
            monoBehaviour.enabled = value;
        }
    }
}
