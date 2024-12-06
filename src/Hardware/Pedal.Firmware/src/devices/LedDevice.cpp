#include "App.h"


void LedDevice::begin(uint8_t pin)
{
    _pin = pin;
    pinMode(pin, OUTPUT);
    off();
}

void LedDevice::on()
{
    log_d("Led ON");
    digitalWrite(_pin, LOW);
}

void LedDevice::off()
{
    log_d("Led OFF");
    digitalWrite(_pin, HIGH);
}

void LedDevice::blink(int timeMs)
{
     TTaskFunction body = [=]() {
        on();
        delay(timeMs);
        off();
    };

    runTask(body);
}