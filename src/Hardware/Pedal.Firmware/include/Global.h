#pragma once

extern Preferences preferences;

extern BatteryDevice battery;
extern LedDevice led;

extern AdcReadTask readTask;
extern BltMonitorTask bltMonitor;


#define HAS_FLAG(a, b) (((int)a & (int)b) != 0)