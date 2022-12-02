public static class JSONStorableExtension
{
    public static void CallActionNullSafe(this JSONStorable storable, string actionName)
    {
        if(storable != null && storable.IsAction(actionName))
        {
            storable.CallAction(actionName);
        }
    }
}
