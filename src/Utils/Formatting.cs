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
            string printName = StripPrefixes(name).PadRight(padRight, ' ');
            return string.Format("{0} {1}", printName, $"{rounded}");
        }

        private static string StripPrefixes(string text)
        {
            string result = StripPrefix(text, "TM_");
            result = StripPrefix(result, "UPR_");
            result = StripPrefix(result, "UPSD_");
            result = StripPrefix(result, "LBACK_");
            result = StripPrefix(result, "LFWD_");
            result = StripPrefix(result, "RLEFT_");
            result = StripPrefix(result, "RRIGHT_");
            return result;
        }

        private static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
        }
    }
}
