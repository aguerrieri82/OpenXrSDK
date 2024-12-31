#pragma once

struct SensorValue {
    long timestamp;
    int value;
    int deltaTime;
};


class BltMonitorTask : public BaseTask,
                       protected BLECharacteristicCallbacks,
                       protected BLEServerCallbacks
{
public:
    virtual void setup() override;
    virtual const char *name() override { return "BltConfig"; }
    void updateSettings(const Settings &value);
    void setValue(const SensorValue &value);
    virtual bool isActive() override { return _server->getConnectedCount() > 0; }

protected:
    virtual void stepWork() override;
    virtual void onConnect(BLEServer *pServer) override;
    virtual void onDisconnect(BLEServer *pServer) override;
    virtual void onWrite(BLECharacteristic *pCharacteristic) override;
    virtual void onRead(BLECharacteristic *pCharacteristic) override;


private:
    BLEServer *_server = nullptr;
    BLEService *_service = nullptr;
    BLECharacteristic *_settings = nullptr;
    BLECharacteristic *_readValue = nullptr;
    BLECharacteristic* _battery = nullptr;
    
    BLEAdvertising *_adv = nullptr;
    long _lastActivityTime = 0;
};
