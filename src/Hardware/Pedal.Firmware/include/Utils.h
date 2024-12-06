#pragma once

typedef std::function<void(void)> TTaskFunction;

TaskHandle_t runTask(TTaskFunction &main, uint32_t stackSize = 10000, uint8_t core = 0);
