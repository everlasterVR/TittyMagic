using static TittyMagic.Utils;

namespace TittyMagic
{
    public class BoolSetting
    {
        public bool prevValue;
        private readonly string _notification;

        public BoolSetting(bool initialValue, string notification = "")
        {
            prevValue = initialValue;
            _notification = notification;
        }

        public bool CheckIfUpdateNeeded(bool value)
        {
            bool result = value && !prevValue;
            if(!value && prevValue && !string.IsNullOrEmpty(_notification))
            {
                LogMessage(_notification);
            }

            prevValue = value;
            return result;
        }
    }
}
