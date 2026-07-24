#include "DisplayManager.h"

DisplayManager::DisplayManager(U8G2& display)
    : _display(display)
{
}

void DisplayManager::begin()
{
    _display.begin();
}

void DisplayManager::clearDisplay()
{
    _display.clearBuffer();
}

void DisplayManager::setFont(const uint8_t* font)
{
    _display.setFont(font);
}

void DisplayManager::print(int x, int y, const char* text)
{
    _display.drawStr(x, y, text);
}

void DisplayManager::print(int x, int y, const String& text)
{
    _display.drawStr(x, y, text.c_str());
}

void DisplayManager::update()
{
    _display.sendBuffer();
}

void DisplayManager::setContrast(uint8_t contrast)
{
    _display.setContrast(contrast);
}

void DisplayManager::setPowerSave(bool on)
{
    _display.setPowerSave(on);
}