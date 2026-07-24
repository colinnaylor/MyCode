#include <Arduino.h>
#include <ArduinoOTA.h>

#include "OTAUpdate.h"
#include "Secrets.h"

namespace
{
    constexpr char OTA_HOSTNAME[] = "plunge-pool-monitor";
}

void initialiseOTA()
{
    ArduinoOTA.setHostname(OTA_HOSTNAME);
    ArduinoOTA.setPassword(OTA_PASSWORD);

    ArduinoOTA.onStart([]()
    {
        Serial.println("OTA update starting...");
    });

    ArduinoOTA.onEnd([]()
    {
        Serial.println();
        Serial.println("OTA update complete");
    });

    ArduinoOTA.onProgress([](
        unsigned int progress,
        unsigned int total)
    {
        const unsigned int percentage =
            progress / (total / 100);

        Serial.printf(
            "OTA progress: %u%%\r",
            percentage
        );
    });

    ArduinoOTA.onError([](ota_error_t error)
    {
        Serial.printf(
            "OTA error [%u]: ",
            error
        );

        switch (error)
        {
            case OTA_AUTH_ERROR:
                Serial.println("Authentication failed");
                break;

            case OTA_BEGIN_ERROR:
                Serial.println("Could not begin update");
                break;

            case OTA_CONNECT_ERROR:
                Serial.println("Connection failed");
                break;

            case OTA_RECEIVE_ERROR:
                Serial.println("Receive failed");
                break;

            case OTA_END_ERROR:
                Serial.println("Could not finish update");
                break;

            default:
                Serial.println("Unknown error");
                break;
        }
    });

    ArduinoOTA.begin();

    Serial.println("OTA updates enabled");
    Serial.print("OTA hostname: ");
    Serial.println(OTA_HOSTNAME);
}

void handleOTA()
{
    ArduinoOTA.handle();
}