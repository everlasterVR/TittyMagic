namespace TittyMagic
{
    internal static class WaitStatus
    {
        public const int DONE = 0;
        public const int WAITING = 1;
    }

    internal static class RefreshStatus
    {
        public const int DONE = 0;
        public const int PRE_REFRESH_STARTED = 1;
        public const int PRE_REFRESH_OK = 2;
        public const int MASS_STARTED = 3;
        public const int MASS_OK = 4;
        public const int NEUTRALPOS_STARTED = 5;
        public const int NEUTRALPOS_OK = 6;
    }
}
