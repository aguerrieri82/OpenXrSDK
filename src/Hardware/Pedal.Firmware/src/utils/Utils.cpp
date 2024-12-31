#include "App.h"
#include "mbedtls/aes.h"
#include "mbedtls/base64.h"

String toHex(byte *buffer, int size)
{
    String result;

    char byteDigit[3];

    for (int i = 0; i < size; i++)
    {
        int item = buffer[i];

        sprintf(byteDigit, "%x", item);
        if (item < 16)
        {
            byteDigit[1] = byteDigit[0];
            byteDigit[0] = '0';
        }
        byteDigit[2] = 0;

        result += byteDigit;
    }

    return result;
}

void runTaskProc(void *param)
{
    log_d("task ready");
    
    auto entry = (TTaskFunction *)param;
    
    (*entry)();

    delete entry;

    log_d("delete task");
    vTaskDelete(NULL);

    log_d("task deleted");
}

TaskHandle_t runTask(TTaskFunction &main, uint32_t stackSize, uint8_t core)
{
    TaskHandle_t taskId;

    auto *taskEntry = new TTaskFunction(std::move(main));

    xTaskCreatePinnedToCore(
        runTaskProc,
        "runTask",
        stackSize,
        taskEntry,
        1,
        &taskId,
        core);
    return taskId;
}
