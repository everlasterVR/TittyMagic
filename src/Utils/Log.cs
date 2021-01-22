namespace everlaster
{
    public static class Log
    {
        public static void Error(string message, string className = nameof(TittyMagic))
        {
            SuperController.LogError($"{nameof(everlaster)}.{className}: {message}");
        }

        public static void Message(string message, string className = nameof(TittyMagic))
        {
            SuperController.LogMessage($"{nameof(everlaster)}.{className}: {message}");
        }

        public static void AppendTo(JSONStorableString jss, string text)
        {
            jss.SetVal("\n" + text + "\n" + jss.val);
        }
    }
}
