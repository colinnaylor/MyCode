#include <Arduino.h>
#include <SPI.h>
#include <U8g2lib.h>
#include <OneWire.h>
#include <DallasTemperature.h>

// ---------- OLED ----------
U8G2_SSD1309_128X64_NONAME0_F_4W_HW_SPI display(
  U8G2_R1,
  /* CS    */ 5,
  /* DC    */ 17,
  /* RESET */ 16
);

// ---------- Temperature ----------
#define ONE_WIRE_BUS 4

OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);

float averageTemp = 0;
float testTemp = 0;
bool firstReading = true;

void setup()
{
  display.begin();
  sensors.begin();
}

void loop()
{
  updateTemperature();

  display.clearBuffer();

  display.setFont(u8g2_font_6x10_tf);
  display.drawStr(10,10,"Plunge");
  display.drawStr(10,20,"Pool");
  // display.setFont(u8g2_font_6x10_tf);
  display.drawUTF8(50,20,"°C");

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
      display.setFont(u8g2_font_logisoso32_tf);
      display.drawStr(0, 74, tensText);

      display.setFont(u8g2_font_logisoso78_tn);
      display.drawStr(16, 120, unitsText);
  }else{
    display.setFont(u8g2_font_logisoso92_tn);
    display.drawStr(2, 125, unitsText);
  }


  display.sendBuffer();

  delay(1000);
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
