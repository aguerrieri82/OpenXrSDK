#include "App.h"

int averageADC(int pin, int samples = 10) {

    int sum = 0;
    
    for (int i = 0; i < samples; i++)
    {
        sum += analogRead(pin);
        delayMicroseconds(50); 
    }

    return sum / samples;
}

void AdcReadTask::stepWork()
{ 
    #ifdef BUILD_RELEASE

    if (!bltMonitor.isActive())
    {
        suspend(100);
        return;
    }
    
    #endif

    int adcValue = averageADC(ADC_PIN);
    
    if (settings.mode == 'H')
    {
        if (_state == 0)
        {
            if (adcValue > settings.rampUp)
            {
                _state = 1;
                _upTime = millis();
                log_d("Ramp Up");
            }
        }
        else if (_state == 1)
        {
            if (adcValue > settings.rampHit)
            {
                SensorValue value;

                value.timestamp = millis();
                value.deltaTime = value.timestamp - _upTime;
                value.value = 1;

                log_i("Hit, dt: %d", value.deltaTime);

                led.blink(200);

                bltMonitor.setValue(value);

                _state = 2;
            }
        }
        else if (_state == 2)
        {
            if (adcValue < settings.rampUp)
            {
                _state = 0;
                log_d("Ramp Down");
            }
        }
    }
    else
    {
        SensorValue value;

        value.timestamp = millis();
        value.deltaTime = value.timestamp - _upTime;
        value.value = adcValue;

        bltMonitor.setValue(value);
    }


    auto delayMs = std::min(1, 1000 / settings.sampleRate);

    suspend(delayMs);
    
    //esp_sleep_enable_timer_wakeup(delayMs * 1000);
    //esp_deep_sleep_start();
}

void AdcReadTask::setup()
{
    _state = 0;
}