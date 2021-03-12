namespace TittyMagic
{
    public static class Log
    {
        public static void Error(string message, string name = "")
        {
            SuperController.LogError(Format(message, name));
        }

        public static void Message(string message, string name = "")
        {
            SuperController.LogMessage(Format(message, name));
        }

        private static string Format(string message, string name)
        {
            return $"{nameof(TittyMagic)} v{Script.version}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";
        }
    }
}
