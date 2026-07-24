#include <Arduino.h>
#include <SPI.h>
#include <U8g2lib.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include <WifiConnection.h>
#include <TimeManager.h>
#include "WebPage.h"
#include "CloudUpload.h"
#include "OTAUpdate.h"
#include "DisplayManager.h"

// ---------- OLED ----------
U8G2_SSD1309_128X64_NONAME0_F_4W_HW_SPI display(
  U8G2_R1,
  /* CS    */ 5,
  /* DC    */ 17,
  /* RESET */ 16
);

DisplayManager displayManager(display);

// ---------- Temperature ----------
#define ONE_WIRE_BUS 4

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

float averageTemp = 0;
float testTemp = 0;
bool firstReading = true;
tm currentTime;
constexpr unsigned long CLOUD_UPLOAD_INTERVAL =
    5UL * 60UL * 1000UL;

unsigned long lastCloudUpload = 0;
bool cloudHasUploaded = false;
unsigned long lastDisplayUpdate = 0;

constexpr int DIM_HOUR = 19;
constexpr int OFF_HOUR = 0;
constexpr int ON_HOUR  = 8;

constexpr uint8_t DAY_CONTRAST = 255;
constexpr uint8_t EVENING_CONTRAST = 1;

enum DisplayMode
{
    Unknown,
    Day,
    Evening,
    Off
};

DisplayMode currentDisplayMode = DisplayMode::Unknown;

void setup()
{
  Serial.begin(115200);

  displayManager.begin();
  displayManager.setContrast(DAY_CONTRAST);
  sensors.begin();

  const char* ssid = connectToWiFi(displayManager);
    displayManager.clearDisplay();
    displayManager.setFont(u8g2_font_5x7_tf);
  displayManager.print(10,30,ssid);

  getTime();
  initialiseOTA();
  initialiseWebServer();

  displayManager.update();
  delay(3000);

}

void loop()
{
  handleOTA();
  handleWebServer();

    const unsigned long now = millis();

    if (now - lastDisplayUpdate >= 1000)
    {
        lastDisplayUpdate = now;

        updateDisplayBrightness();
        updateTemperature();
        updateCloudTemperature();

        drawDisplay();
    }

    delay(5);
}

void drawDisplay()
{
  displayManager.clearDisplay();

  displayManager.setFont(u8g2_font_6x10_tf);
  displayManager.print(10,10,"Plunge");
  displayManager.print(10,20,"Pool");
  displayManager.print(50,20,"°C");

  int displayTemp = round(averageTemp);
  // For testing
  // testTemp += 1;
  // displayTemp = testTemp;

  int tens = displayTemp / 10;
  int units = displayTemp % 10;

  char tensText[2];
  char unitsText[2];

  sprintf(tensText, "%d", tens);
  sprintf(unitsText, "%d", units);

  if (displayTemp >= 10)
  {
      displayManager.setFont(u8g2_font_logisoso32_tf);
      displayManager.print(0, 74, tensText);

      displayManager.setFont(u8g2_font_logisoso78_tn);
      displayManager.print(16, 120, unitsText);
  }else{
    displayManager.setFont(u8g2_font_logisoso92_tn);
    displayManager.print(2, 125, unitsText);
  }

  displayManager.update();
}

void getTime(){
  initialiseTime();
  
  displayManager.setFont(u8g2_font_5x7_tf);

  if (getCurrentTime(currentTime))
  {
      char timeString[6];

      strftime(timeString, sizeof(timeString), "%H:%M", &currentTime);

      displayManager.print(10, 50, timeString);
  }
}

void updateTemperature()
{
    sensors.requestTemperatures();
    float temp = sensors.getTempCByIndex(0);

    if (firstReading)
    {
        averageTemp = temp;
        firstReading = false;
    }
    else
    {
        // 90% old value, 10% new value
        averageTemp = averageTemp * 0.9 + temp * 0.1;
    }
}

void setDisplayMode(DisplayMode newMode)
{
    if (newMode == currentDisplayMode)
        return;

    currentDisplayMode = newMode;

    switch (newMode)
    {
        case DisplayMode::Day:
            Serial.println("Applying daytime brightness");
            displayManager.setPowerSave(0);
            displayManager.setContrast(DAY_CONTRAST);
            break;

        case DisplayMode::Evening:
            Serial.println("Applying evening brightness");
            displayManager.setPowerSave(0);
            displayManager.setContrast(EVENING_CONTRAST);
            break;

        case DisplayMode::Off:
            Serial.println("Brightness off");
            displayManager.setPowerSave(1);
            break;
    }
}

void updateDisplayBrightness()
{
    struct tm currentTime;

    if (!getCurrentTime(currentTime))
        return;

    int hour = currentTime.tm_hour;

    if (hour >= OFF_HOUR && hour < ON_HOUR)
    {
        setDisplayMode(DisplayMode::Off);
    }
    else if (hour >= DIM_HOUR)
    {
        setDisplayMode(DisplayMode::Evening);
    }
    else
    {
        setDisplayMode(DisplayMode::Day);
    }
}

void updateCloudTemperature()
{
    if (firstReading)
        return;

    unsigned long now = millis();

    if (!cloudHasUploaded ||
        now - lastCloudUpload >= CLOUD_UPLOAD_INTERVAL)
    {
        if (uploadTemperature(averageTemp))
        {
            lastCloudUpload = now;
            cloudHasUploaded = true;
        }
    }
}


