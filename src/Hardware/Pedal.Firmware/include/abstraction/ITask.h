#pragma once

enum TaskMode
{
	TASK_MAIN,
	TASK_PARALLEL
};

class ITask
{
public:
	virtual void setup() = 0;

	virtual void step() = 0;

	virtual bool isActive() { return false; };

	virtual void run() = 0;
	
	virtual long nextStepTime() = 0;

	virtual TaskMode mode() { return TASK_MAIN; };

	virtual const char *name() = 0;

	bool isDisabled = false;
};