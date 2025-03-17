using System.Collections.Generic;

namespace Lazy.Timer
{
    public static class TimerConstant
    {
        public static readonly List<string> NtpServerList =
            new()
            {
                "pool.ntp.org",
                "time.google.com",
                "time.aws.com",
                "time.facebook.com",
                "time.apple.com",
                "time.windows.com",
                "asia.pool.ntp.org",
                "south-america.pool.ntp.org",
                "north-america.pool.ntp.org",
                "africa.pool.ntp.org",
                "europe.pool.ntp.org",
                "oceania.pool.ntp.org",
            };
    }
}
