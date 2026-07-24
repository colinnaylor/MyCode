#include <Arduino.h>
#include <time.h>

#include "TimeManager.h"

namespace
{
    constexpr char TIME_ZONE[] =
        "GMT0BST,M3.5.0/1,M10.5.0";

    constexpr char NTP_SERVER_1[] =
        "pool.ntp.org";

    constexpr char NTP_SERVER_2[] =
        "time.nist.gov";
}

bool initialiseTime()
{
    // Automatically handles UK GMT/BST changes.
    configTzTime(
        TIME_ZONE,
        NTP_SERVER_1,
        NTP_SERVER_2
    );

    struct tm timeInfo;

    // Wait up to approximately 10 seconds for NTP.
    for (int attempt = 0; attempt < 20; attempt++)
    {
        if (getLocalTime(&timeInfo, 500))
        {
            return true;
        }

        delay(100);
    }

    return false;
}

bool getCurrentTime(struct tm& timeInfo)
{
    return getLocalTime(&timeInfo, 100);
}
