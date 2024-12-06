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
    int adcValue = averageADC(ADC_PIN);
    
    if (settings.mode == 'H')
    {
        if (_state == 0)
        {
            if (adcValue > settings.rampUp)
            {
              _state = 1;
              _upTime = millis();
            }
        }
        else if (_state == 1)
        {
            if (adcValue > settings.rampHit)
            {
                _state = 2;
                SensorValue value;

                value.timestamp = millis();
                value.deltaTime = value.timestamp - _upTime;
                value.value = 1;

                log_d("Hit, dt: %d", value.deltaTime);

                led.blink(200);

                bltMonitor.setValue(value);
            }
        }
        else if (_state == 2)
        {
            if (adcValue < settings.rampDown)
            {
              _state = 0;
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

    this->suspend(std::min(1, 1000 / settings.sampleRate));
}

void AdcReadTask::setup()
{
    _state = 0;
}