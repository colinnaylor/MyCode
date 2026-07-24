#ifndef DISPLAY_MANAGER_H
#define DISPLAY_MANAGER_H

#include <U8g2lib.h>

class DisplayManager
{
public:
    explicit DisplayManager(U8G2& display);

    void begin();

    void clearDisplay();

    void setFont(const uint8_t* font);

    void print(int x, int y, const char* text);

    void print(int x, int y, const String& text);

    void update();

    void setContrast(uint8_t contrast);

    void setPowerSave(bool on);

private:
    U8G2& _display;
};

#endif