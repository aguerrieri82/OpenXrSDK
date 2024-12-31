#pragma once

class Lock
{
public:
    inline Lock(SemaphoreHandle_t semHandler)
    {
        _semHandler = semHandler;
        
        log_d("Sem Ask %d", (int)_semHandler);
        xSemaphoreTake(_semHandler, portMAX_DELAY);
        log_d("Sem Take %d", (int)_semHandler);
    }

    inline ~Lock()
    {
        xSemaphoreGive(_semHandler);
        log_d("Sem Give %d", (int)_semHandler);
    }

private:
    SemaphoreHandle_t _semHandler;

};