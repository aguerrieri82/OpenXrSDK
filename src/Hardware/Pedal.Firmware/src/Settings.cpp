#include "App.h"

Settings settings;

void loadSettings() 
{
    Settings curSettings;

    size_t result = preferences.getBytes("settings", &curSettings, sizeof(Settings));

    if (curSettings.key != SETTINGS_KEY || curSettings.size != sizeof(Settings))
    {
        log_w("No Setting in EEPROM, use default");

        memset(&settings, 0, sizeof(Settings));

        settings.size = sizeof(Settings);
        settings.key = SETTINGS_KEY;
        settings.mode ='V';
        settings.sampleRate = 500;
        settings.rampUp = 800;
        settings.rampHit = 1200;
        settings.rampDown = 1100;

        saveSettings();
    }
    else
    {
        log_i("Settings loaded");
        settings = curSettings;
    }    
}

void saveSettings() 
{
    preferences.putBytes("settings", &settings, sizeof(Settings));

    log_i("Settings saved");
}

