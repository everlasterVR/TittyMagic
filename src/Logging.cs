namespace everlaster
{
    public static class Log
    {
        public static void Error(string message)
        {
            SuperController.LogError($"{nameof(everlaster)}.{nameof(TittyMagic)}: {message}");
        }

        public static void Message(string message)
        {
            SuperController.LogMessage($"{nameof(everlaster)}.{nameof(TittyMagic)}: {message}");
        }

        //TODO reimplement debug functions for logging stuff to UI
    }
}
