#include <Arduino.h>
#include <WiFi.h>
#include <WebServer.h>

#include "WebPage.h"
#include "TimeManager.h"

extern float averageTemp;

namespace
{
    WebServer server(80);
}

void showHomePage()
{
    struct tm timeInfo;
    char timeText[6] = "--:--";

    if (getCurrentTime(timeInfo))
    {
        strftime(
            timeText,
            sizeof(timeText),
            "%H:%M",
            &timeInfo
        );
    }

    const int temperature = round(averageTemp);

    String page;
    page.reserve(1400);

    page += R"HTML(
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta http-equiv="refresh" content="10">
<title>Plunge Pool</title>
<style>
body {
    margin: 0;
    min-height: 100vh;
    display: grid;
    place-items: center;
    background: #142735;
    color: white;
    font-family: Arial, sans-serif;
}
.panel {
    width: min(420px, 82vw);
    padding: 36px;
    text-align: center;
    background: #1d3a4e;
    border-radius: 18px;
}
h1 {
    margin: 0 0 24px;
    font-size: 28px;
}
.temperature {
    font-size: 96px;
    font-weight: bold;
}
.time {
    margin-top: 20px;
    font-size: 20px;
}
</style>
</head>
<body>
<div class="panel">
<h1>Plunge Pool</h1>
<div class="temperature">
)HTML";

    page += temperature;

    page += R"HTML(&deg;C</div>
<div class="time">Updated )HTML";

    page += timeText;

    page += R"HTML(</div>
</div>
</body>
</html>
)HTML";

    server.send(200, "text/html", page);
}

void initialiseWebServer()
{
    server.on("/", HTTP_GET, showHomePage);
    server.begin();

    Serial.println("Web server started");
    Serial.print("Browse to http://");
    Serial.println(WiFi.localIP());
}

void handleWebServer()
{
    server.handleClient();
}