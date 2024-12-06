#include "App.h"


void BltMonitorTask::setup()
{

    BLEDevice::init(DEVICE_NAME);

    log_d("%s", BLEDevice::getAddress().toString());

    _server = BLEDevice::createServer();
    _server->setCallbacks(this);

    _service = _server->createService(BLE_SERVICE_UUID);

    //Battery
    _battery = _service->createCharacteristic(
        BLE_BATTERY_UUID,
        BLECharacteristic::PROPERTY_READ);

    BLEDescriptor *batteryDesc = new BLEDescriptor("2901");
    batteryDesc->setValue("Battery");
    _battery->addDescriptor(batteryDesc);
    _battery->setCallbacks(this);

    // Settings
    _settings = _service->createCharacteristic(
        BLE_SETTINGS_UUID,
        BLECharacteristic::PROPERTY_READ |
            BLECharacteristic::PROPERTY_WRITE);

    BLEDescriptor *settingsDesc = new BLEDescriptor("2901");
    settingsDesc->setValue("Settings");
    _settings->addDescriptor(settingsDesc);
    _settings->setCallbacks(this);

    updateSettings(settings);


    // Read Card
    _readValue = _service->createCharacteristic(
        BLE_VALUE_UUID,
        BLECharacteristic::PROPERTY_READ |
            BLECharacteristic::PROPERTY_NOTIFY);

    BLEDescriptor *readCardDesc = new BLEDescriptor("2901");
    readCardDesc->setValue("Value");

    _readValue->addDescriptor(readCardDesc);
    _readValue->addDescriptor(new BLE2902());

    // Start
    _service->start();

    _adv = BLEDevice::getAdvertising();
    _adv->addServiceUUID(BLE_SERVICE_UUID);
    _adv->setScanResponse(true);
    _adv->setMinPreferred(0x12);
    _server->startAdvertising();
}

void BltMonitorTask::onConnect(BLEServer *pServer)
{
    log_d("BLE Client Connected");
    _lastActivityTime = millis();
}

void BltMonitorTask::onDisconnect(BLEServer *pServer)
{
    log_d("BLE: %i", _server->getConnectedCount());
    log_d("BLE Client Disconnect");

    if (_server->getConnectedCount() <= 1)
        _server->startAdvertising();

    _lastActivityTime = millis();
}

void BltMonitorTask::onRead(BLECharacteristic *pCharacteristic)
{
    if (pCharacteristic == _battery)
    {
        auto raw = battery.readRaw();
        pCharacteristic->setValue(raw);
    }

    _lastActivityTime = millis();
}

void BltMonitorTask::onWrite(BLECharacteristic *pCharacteristic)
{
    if (pCharacteristic == _settings)
    {
        Settings* newSettings = (Settings*)pCharacteristic->getData();
        settings =*newSettings;
        saveSettings();
    }

    _lastActivityTime = millis();
}


void BltMonitorTask::updateSettings(const Settings &value)
{
    if (_settings == nullptr)
        return;

    _settings->setValue((uint8_t*) &value, sizeof(value));
}

void BltMonitorTask::setValue(const SensorValue &value)
{
    if (_server->getConnectedCount() == 0)
        return;

    _readValue->setValue((uint8_t*) &value, sizeof(value));

    _readValue->notify();
}


void BltMonitorTask::stepWork()
{
    if (_server == nullptr)
        return;

    suspend(8000);

    if (BLE_DISCONNECT_TIME > 0 &&
        millis() - _lastActivityTime > BLE_DISCONNECT_TIME)
    {
        if (_server->getConnectedCount() > 0)
        {
            log_d("BLE Disconnecting");
            for (auto const &device : _server->getPeerDevices(true))
                _server->disconnect(device.first);
        }
        else
            _lastActivityTime = millis();
    }

    log_d("BLE: %i", _server->getConnectedCount());
}