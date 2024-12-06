#pragma once

#include <iostream>
#include <string>
#include <cctype>
#include <sstream>
#include <iomanip>
#include <vector>

#include <Arduino.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>
#include <Preferences.h>

#include "driver/rtc_io.h"

#include "Config.h"

#include "Settings.h"
#include "Lock.h"
#include "Utils.h"

#include "abstraction/ITask.h"

#include "devices/BatteryDevice.h"
#include "devices/LedDevice.h"

#include "tasks/BaseTask.h"
#include "tasks/AdcReadTask.h"
#include "tasks/BltMonitorTask.h"
#include "tasks/PowerManagerTask.h"


#include "Global.h"
