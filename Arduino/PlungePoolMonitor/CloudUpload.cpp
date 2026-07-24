#include <Arduino.h>
#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <HTTPClient.h>

#include "CloudUpload.h"
#include "TimeManager.h"
#include "Secrets.h"

namespace
{
  const char* WORKER_URL =
    "https://plungepoolreadings.colinnaylor.workers.dev/temperature";
}

bool uploadTemperature(float temperature)
{
    if (WiFi.status() != WL_CONNECTED)
    {
        Serial.println("Cloud upload skipped: Wi-Fi disconnected");
        return false;
    }

    WiFiClientSecure client;

    // Simple initial setup. This encrypts the connection but does not
    // verify the server certificate. We can add CA verification later.
    client.setInsecure();

    HTTPClient http;

    if (!http.begin(client, WORKER_URL))
    {
        Serial.println("Unable to initialise HTTPS request");
        return false;
    }

    http.addHeader("Content-Type", "application/json");
    http.addHeader(
        "Authorization",
        String("Bearer ") + CLOUD_UPLOAD_TOKEN
    );

    struct tm timeInfo;
    char deviceTime[20] = "";

    if (getCurrentTime(timeInfo))
    {
        strftime(
            deviceTime,
            sizeof(deviceTime),
            "%Y-%m-%d %H:%M",
            &timeInfo
        );
    }

    String json;
    json.reserve(100);

    json += "{\"temperature\":";
    json += String(temperature, 1);
    json += ",\"deviceTime\":\"";
    json += deviceTime;
    json += "\"}";

    const int responseCode = http.POST(json);

    Serial.print("Cloud upload response: ");
    Serial.println(responseCode);

    if (responseCode > 0)
    {
        Serial.println(http.getString());
    }
    else
    {
        Serial.println(http.errorToString(responseCode));
    }

    http.end();

    return responseCode >= 200 && responseCode < 300;
}