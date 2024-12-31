#include "App.h"


void PowerManagerTask::stepWork()
{
	long time = millis();

	if (_lastBatteryRead == 0 || time - _lastBatteryRead > 500)
	{
		_batteryValue += (battery.readPercentage() - _batteryValue) / 1;

		_lastBatteryRead = time;

		auto raw = battery.readRaw();

		float voltage = battery.readVoltage();

		log_d("battery value: %f - %d - %f", (float)_batteryValue, raw, voltage);
	}

	suspend(500);
}

void PowerManagerTask::setup()
{
	_idleStart = millis();
	_lastBatteryRead = 0;
	_batteryValue = 0;
}