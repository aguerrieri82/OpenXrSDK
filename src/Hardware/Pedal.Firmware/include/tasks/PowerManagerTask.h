#pragma once


class PowerManagerTask : public BaseTask
{
public:
    virtual void setup() override;
    virtual const char *name() override { return "PowerManager"; }
    double getBatteryPerc() { return _batteryValue; }
protected:
    virtual void stepWork() override;

private:
    long _idleStart;
    double _batteryValue = 0;
    long _lastBatteryRead = 0;
};
