#pragma once

class BatteryDevice
{
public:
    void begin(uint8_t vccPin, uint16_t min, uint16_t* map, uint16_t mapLen);

    float readPercentage();
    uint16_t readRaw();
    float readVoltage();
    bool isAttached();

private:
    uint8_t _vccPin;
    uint16_t _mapLen;
    uint16_t _min;
    uint16_t* _map;
};