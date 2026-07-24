#include <stdio.h>
#include <algorithm>
#include <thread>
#include <vector>

#include "astcenc.h"

#ifdef _WINDOWS
	
	#define NOMINMAX

    #include <Windows.h>

    #pragma comment(lib, "winmm.lib")

    #define EXPORT __declspec(dllexport)


#else

    #include <sys/resource.h>
    #include <unistd.h>

    #define EXPORT __attribute__((visibility("default")))
    #define APIENTRY

#endif

#pragma pack(push, 1)

struct astcenc_params
{
    astcenc_profile profile;
    unsigned int block_x;
    unsigned int block_y;
    unsigned int block_z;
    float quality;
    unsigned int flags;
    uint8_t thread_count;
    int8_t thread_priority;
    astcenc_swizzle swizzle;
};

#pragma pack(pop)

static void SetWorkerPriority(int8_t priority)
{
#ifdef _WINDOWS

    SetThreadPriority(GetCurrentThread(), priority);

#elif defined(__ANDROID__)

    int niceValue;

    switch (priority)
    {
        case 0:
            niceValue = 0;
            break;

        case -1:
            niceValue = 10;
            break;

        case -2:
            niceValue = 19;
            break;

        default:
            niceValue = 0;
            break;
    }

    setpriority(PRIO_PROCESS, 0, niceValue);

#endif
}

extern "C"
{
    EXPORT astcenc_error APIENTRY Encode(
        uint8_t* data,
        int width,
        int height,
        int depth,
        astcenc_type dataType,
        astcenc_params& params,
        uint8_t* dst,
        int& dstSize)
    {
        depth = std::max(depth, 1);

        unsigned int block_count_x = ((unsigned int)width + params.block_x - 1) / params.block_x;
        unsigned int block_count_y = ((unsigned int)height + params.block_y - 1) / params.block_y;
        unsigned int block_count_z = ((unsigned int)depth + params.block_z - 1) / params.block_z;

        size_t requiredSize =
            (size_t)block_count_x *
            (size_t)block_count_y *
            (size_t)block_count_z *
            16;

        if (dst == nullptr)
        {
            dstSize = (int)requiredSize;
            return ASTCENC_SUCCESS;
        }

        size_t componentSize =
            dataType == ASTCENC_TYPE_U8 ? 1 :
            dataType == ASTCENC_TYPE_F16 ? 2 :
            dataType == ASTCENC_TYPE_F32 ? 4 : 0;

        if (componentSize == 0)
            return ASTCENC_ERR_BAD_PARAM;

        size_t sliceBytes =
            (size_t)width *
            (size_t)height *
            4 *
            componentSize;

        std::vector<void*> slices((size_t)depth);

        for (int z = 0; z < depth; z++)
            slices[(size_t)z] = data + (size_t)z * sliceBytes;

        astcenc_image image;
        image.dim_x = (unsigned int)width;
        image.dim_y = (unsigned int)height;
        image.dim_z = (unsigned int)depth;
        image.data_type = dataType;
        image.data = slices.data();

        astcenc_config config;

        astcenc_error status = astcenc_config_init(
            params.profile,
            params.block_x,
            params.block_y,
            params.block_z,
            params.quality,
            params.flags,
            &config);

        if (status != ASTCENC_SUCCESS)
            return status;

        astcenc_context* context = nullptr;

        unsigned int threadCount = params.thread_count > 0 ? params.thread_count : 1;

        status = astcenc_context_alloc(&config, threadCount, &context);

        if (status != ASTCENC_SUCCESS)
            return status;

        if (threadCount == 1)
        {
            SetWorkerPriority(params.thread_priority);

            status = astcenc_compress_image(
                context,
                &image,
                &params.swizzle,
                dst,
                (size_t)dstSize,
                0);
        }
        else
        {
            std::vector<std::thread> threads;
            std::vector<astcenc_error> errors(threadCount);

            for (unsigned int i = 0; i < threadCount; i++)
            {
                threads.emplace_back([&, i]()
                {
                    SetWorkerPriority(params.thread_priority);

                    errors[i] = astcenc_compress_image(
                        context,
                        &image,
                        &params.swizzle,
                        dst,
                        (size_t)dstSize,
                        i);
                });
            }

            for (std::thread& thread : threads)
                thread.join();

            status = ASTCENC_SUCCESS;

            for (unsigned int i = 0; i < threadCount; i++)
            {
                if (errors[i] != ASTCENC_SUCCESS)
                {
                    status = errors[i];
                    break;
                }
            }
        }

        astcenc_context_free(context);

        return status;
    }
}