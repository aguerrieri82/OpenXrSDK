#pragma once

class LedDevice
{
public:
    void begin(uint8_t pin);
    void on();
    void off();
    void blink(int timeMs);

private:
    uint8_t _pin;

};