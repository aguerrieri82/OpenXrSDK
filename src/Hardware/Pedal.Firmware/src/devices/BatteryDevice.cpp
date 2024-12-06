#include "App.h"

#define SAMPLE_SIZE 20

#define R1 100000.0
#define R2 100000.0

void BatteryDevice::begin(uint8_t vccPin, uint16_t min, uint16_t* map, uint16_t mapLen)
{
    _vccPin = vccPin;
    _min = min;
    _map = map;
    _mapLen = mapLen;
    analogSetWidth(12);
    pinMode(_vccPin, INPUT_PULLDOWN);
    rtc_gpio_pullup_en((gpio_num_t)_vccPin);
}

float BatteryDevice::readPercentage()
{
    auto raw = readRaw();
    if (raw < _min)
        return 0;
    if (raw >= _min + _mapLen)
        return 100;
    return _map[raw - _min];
}

uint16_t BatteryDevice::readRaw()
{
    uint32_t value = 0;

    for (int i = 0; i < SAMPLE_SIZE; i++)
        value += analogRead(_vccPin);

    return (uint16_t)(value / SAMPLE_SIZE);
}


float BatteryDevice::readVoltage()
{
     auto raw = readRaw();
     float voltageADC = (raw / 4095.0) * 3.3;
     float batteryVoltage = voltageADC * (R1 + R2) / R2;
     return batteryVoltage;
}

bool BatteryDevice::isAttached()
{

    uint16_t values[SAMPLE_SIZE];
    float avg = 0;

    for (int i = 0; i < SAMPLE_SIZE; i++)
    {
        uint16_t value = analogRead(_vccPin);
        avg += value;
        values[i] = value;
        delay(1);
    }

    avg /= SAMPLE_SIZE;

    float stdDev = 0;

    for (int i = 0; i < SAMPLE_SIZE; i++)
        stdDev += pow(values[i] - avg, 2);

    stdDev = sqrt(stdDev / SAMPLE_SIZE);

    //log_d("battery stdev: %f", stdDev);

    return stdDev < 12;
}
