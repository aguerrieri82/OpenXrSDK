#pragma once

class BaseTask : public ITask
{
public:
    BaseTask();
    virtual void step() override;
    inline void suspend() { _suspendCount++; }
    inline void resume() { _suspendCount--; }
    long nextStepTime() override ;
    void suspend(uint32_t ms);
    void cancelSuspend();

    void run() override;
protected:
    virtual void stepWork() = 0;
    bool isCurrentTask();
    long _suspendStart;
    uint32_t _suspendLen;
    uint16_t _suspendCount;
    size_t _stackSize;
    bool _isReady;
    TaskHandle_t _taskId;
};