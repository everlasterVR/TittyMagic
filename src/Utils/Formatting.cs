namespace everlaster
{
    public static class Formatting
    {
        public static string NameValueString(
            string name,
            float value,
            float roundFactor = 1000f,
            int padRight = 0,
            bool normalize = false
        )
        {
            double rounded = Calc.RoundToDecimals(value, roundFactor);
            string printName = StripPrefix(name, "TM_").PadRight(padRight, ' ');
            string printValue = normalize ? NormalizeNumberFormat(rounded) : $"{rounded}";
            return string.Format("{0} {1}", printName, printValue);
        }

        public static string StripPrefix(string text, string prefix)
        {
            return text.StartsWith(prefix) ? text.Substring(prefix.Length) : text;
        }

        public static string NormalizeNumberFormat(double value)
        {
            string formatted = string.Format("{0:000.00}", value);
            return value >= 0 ? $" {formatted}" : formatted;
        }
    }
}
