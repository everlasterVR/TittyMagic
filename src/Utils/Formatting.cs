namespace everlaster
{
    public static class Formatting
    {
        public static string NameValueString(
            string name,
            float value,
            float roundFactor = 1000f,
            int padRight = 0
        )
        {
            float rounded = Calc.RoundToDecimals(value, roundFactor);
            string printName = StripPrefix(name, "TM_").PadRight(padRight, ' ');
            return string.Format("{0} {1}", printName, $"{rounded}");
        }

        public static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
        }
    }
}
