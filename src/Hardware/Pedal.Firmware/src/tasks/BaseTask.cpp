#include "App.h"

void RunTask(void *pvParameters)
{
    BaseTask *task = (BaseTask *)pvParameters;

    while (true)
        task->step();
}

BaseTask::BaseTask()
{
    _suspendStart = 0;
    _suspendLen = 0;
    _suspendCount = 0;
    _isReady = false;
    _stackSize = 10000;
    _taskId = xTaskGetCurrentTaskHandle();
}

void BaseTask::step()
{
    if (mode() == TASK_MAIN)
        _taskId = xTaskGetCurrentTaskHandle();

    _isReady = true;

    if (_suspendCount > 0)
    {
        yield();
        return;
    }

    if (_suspendLen > 0 && millis() - _suspendStart < _suspendLen)
    {
        yield();
        return;
    }

    stepWork();
}

long BaseTask::nextStepTime()
{
    return _suspendLen == 0 ? millis() : _suspendLen + _suspendStart;
}

void BaseTask::cancelSuspend()
{
 
    _suspendLen = 0;
    _suspendStart = 0;

    if (!isCurrentTask() && _isReady && _taskId != NULL)
        xTaskAbortDelay(_taskId);
}

bool BaseTask::isCurrentTask()
{
    return xTaskGetCurrentTaskHandle() == _taskId;
}

void BaseTask::suspend(uint32_t ms)
{
    if (mode() == TASK_PARALLEL)
        delay(ms);
    else
    {
        _suspendLen = ms;
        _suspendStart = millis();
    }
}

void BaseTask::run()
{
    setup();

    xTaskCreatePinnedToCore(
        RunTask,
        "Task2",
        _stackSize,
        this,
        1,
        &_taskId,
        1);
}