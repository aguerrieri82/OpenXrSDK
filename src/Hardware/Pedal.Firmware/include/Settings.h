#pragma once

#define SETTINGS_KEY 1397052500

struct Settings {
    size_t size;
    int key;
    char mode;
    int sampleRate;
    int rampUp;
    int rampHit;
    int rampDown;
};

void loadSettings();

void saveSettings();

extern Settings settings;