#include "App.h"

uint16_t PROGMEM BatteryData[] = {
    0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 10, 10, 11, 11, 12, 12, 12, 13, 14, 14, 15, 16, 16, 17, 18, 19, 19, 19, 20, 21, 22, 22, 23, 24, 25, 25, 26, 26, 26, 27, 27, 28, 28, 28, 28, 28, 29, 29, 29, 30, 30, 30, 31, 31, 31, 31, 31, 32, 32, 32, 33, 33, 33, 33, 34, 34, 34, 34, 35, 35, 35, 35, 36, 36, 36, 37, 37, 37, 37, 38, 38, 38, 38, 39, 39, 39, 39, 40, 40, 40, 40, 40, 41, 41, 41, 42, 42, 42, 42, 42, 43, 43, 43, 44, 44, 44, 44, 44, 45, 45, 45, 46, 46, 46, 47, 47, 48, 49, 49, 49, 50, 50, 51, 52, 53, 53, 54, 54, 55, 56, 57, 57, 58, 58, 59, 60, 60, 61, 61, 62, 63, 63, 64, 64, 66, 66, 67, 67, 68, 68, 69, 69, 70, 70, 71, 71, 72, 72, 73, 73, 73, 74, 74, 74, 75, 75, 75, 75, 76, 76, 76, 76, 77, 77, 77, 77, 78, 78, 78, 78, 79, 79, 79, 79, 80, 80, 80, 80, 80, 80, 81, 81, 81, 81, 82, 82, 82, 82, 83, 83, 83, 83, 83, 83, 84, 84, 84, 84, 84, 85, 85, 85, 85, 85, 86, 86, 86, 86, 86, 86, 87, 87, 87, 87, 87, 87, 88, 88, 88, 88, 88, 88, 88, 89, 89, 89, 89, 89, 89, 89, 89, 89, 90, 90, 90, 90, 90, 90, 90, 90, 90, 90, 91, 91, 91, 91, 91, 91, 91, 91, 91, 91, 91, 92, 92, 92, 92, 92, 92, 92, 92, 92, 93, 93, 93, 93, 93, 93, 93, 93, 93, 94, 94, 94, 94, 94, 94, 94, 94, 94, 95, 95, 95, 95, 95, 95, 95, 95, 95, 96, 96, 96, 96, 96, 96, 96, 96, 97, 97, 97, 97, 97, 97, 97, 97, 98, 98, 98, 98, 98, 98, 98, 98, 99, 99, 99, 99, 99, 99, 99, 100, 100, 100, 100
};

Preferences preferences;

BatteryDevice battery;
LedDevice led;

AdcReadTask readTask;
BltMonitorTask bltMonitor;
PowerManagerTask powerTask;

ITask *Tasks[] = {
    &readTask,
    &bltMonitor,
    &powerTask
};

const uint8_t TaskCount = sizeof(Tasks) / sizeof(ITask *);

void setup()
{
    Serial.begin(SERIAL_SPEED);

    led.begin(LED_PIN);


    battery.begin(BATTERY_VCC, 1995, BatteryData, sizeof(BatteryData) / 2);

    preferences.begin("pedal-control", false);

    loadSettings();

    for (int i = 0; i < TaskCount; i++)
    {
        if (Tasks[i]->isDisabled)
            continue;

        log_d("Setup %s, heap: %d", Tasks[i]->name(), ESP.getFreeHeap());

        if (Tasks[i]->mode() == TASK_PARALLEL)
            Tasks[i]->run();
        else
            Tasks[i]->setup();

        log_d("Setup End %s, heap: %d", Tasks[i]->name(), ESP.getFreeHeap());
    }

    led.blink(1000);
}

void loop()
{
    long nextExecTime = 0;

    for (int i = 0; i < TaskCount; i++)
    {
        if (Tasks[i]->mode() == TASK_MAIN && !Tasks[i]->isDisabled)
        {
            Tasks[i]->step();

            long taskNext = Tasks[i]->nextStepTime();
            if (nextExecTime == 0 || taskNext < nextExecTime)
                nextExecTime = taskNext;
        }
    }

    if (nextExecTime > 0)
    {
        long delta = nextExecTime - millis();

        if (delta > 0)
        {
            //log_d("sleep %d", delta);
            delay(delta);
        }
    }
}
