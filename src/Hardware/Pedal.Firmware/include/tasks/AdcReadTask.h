#pragma once


class AdcReadTask : public BaseTask
{
public:
    virtual void setup() override;
    virtual const char *name() override { return "AdcRead"; }
protected:
    virtual void stepWork() override;

private:    
    int _state;
    long _upTime;
};
