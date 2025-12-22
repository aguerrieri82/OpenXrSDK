
#include <stdio.h>

#include "astcenc.h"


#ifdef _WINDOWS

	#pragma comment(lib, "winmm.lib")

	#define EXPORT __declspec(dllexport)

	#define APIENTRY __cdecl

#else

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
	astcenc_swizzle swizzle;
};

#pragma pack(pop)

extern "C"
{
	EXPORT astcenc_error APIENTRY Encode(uint8_t* data, int width, int height, astcenc_type dataType, astcenc_params &params, uint8_t* dst, int &dstSize)
	{
		if (dst == nullptr)
		{
			unsigned int block_count_x = (width + params.block_x - 1) / params.block_x;
			unsigned int block_count_y = (height + params.block_y - 1) / params.block_y;

			dstSize = block_count_x * block_count_y * 16;

			return ASTCENC_SUCCESS;
		}
		
		astcenc_image image;
		image.dim_x = width;
		image.dim_y = height;
		image.dim_z = 1;
		image.data_type = dataType;
		image.data = (void**)&data; 

		astcenc_config config;
		astcenc_error status;
		status = astcenc_config_init(params.profile, params.block_x, params.block_y, params.block_z, params.quality, 0, &config);
		if (status != ASTCENC_SUCCESS)
			return status;

		astcenc_context* context;
		status = astcenc_context_alloc(&config, params.thread_count, &context);
		if (status != ASTCENC_SUCCESS)
			return status;

		status = astcenc_compress_image(context, &image, &params.swizzle, dst, dstSize, 0);

		astcenc_context_free(context);

		return status;
	}
}
