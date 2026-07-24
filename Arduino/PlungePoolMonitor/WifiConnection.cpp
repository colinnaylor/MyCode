#include <WiFi.h>
#include "WifiConnection.h"
#include <U8g2lib.h>
#include "DisplayManager.h"
#include "Secrets.h"

const char* connectToWiFi(DisplayManager& displayManager)
{
    WiFi.mode(WIFI_STA);

    for (int network = 0; network < WIFI_NETWORK_COUNT; network++)
    {
        displayManager.clearDisplay();
        displayManager.setFont(u8g2_font_5x7_tf);
        displayManager.print(10,10,"Connecting");
        displayManager.print(10,30,WIFI_SSIDS[network]);
        displayManager.update();

        Serial.print("Connecting to ");
        Serial.println(WIFI_SSIDS[network]);

        WiFi.begin(WIFI_SSIDS[network], WIFI_PASSWORDS[network]);

        unsigned long started = millis();

        while (WiFi.status() != WL_CONNECTED &&
               millis() - started < 10000)
        {
            delay(500);
            String time = String((millis() - started) / 1000);
            displayManager.print(10,50,time);
            displayManager.update();
            Serial.print(".");
        }

        Serial.println();

        if (WiFi.status() == WL_CONNECTED)
        {
            Serial.println("Connected!");
            Serial.print("IP address: ");
            Serial.println(WiFi.localIP());

            return WIFI_SSIDS[network];
        }

        Serial.println("Connection timed out.");
        WiFi.disconnect(true);
        delay(1000);
    }

    return "No conn";
}
